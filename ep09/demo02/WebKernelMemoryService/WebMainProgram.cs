using System.Globalization;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.InteractiveSetup;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Service.AspNetCore;

using WebKernelMemoryService.HttpFilters;
using WebKernelMemoryService.Internals;

namespace WebKernelMemoryService;

public static class WebMainProgram
{
    private static readonly DateTimeOffset s_start = DateTimeOffset.UtcNow;

    public static void Main(string[] args)
    {
        SensitiveDataLogger.Enabled = true;
        SensitiveDataLogger.LoggingLevel = LogLevel.Trace;

        // *************************** CONFIG WIZARD ***************************

        // Run `dotnet run setup` to run this code and set up the service
        if (new[] { "setup", "--setup", "config" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
        {
            Program.Main(args.Skip(1).ToArray());
        }

        // Run `dotnet run check` to run this code and analyze the service configuration
        if (new[] { "check", "--check" }.Contains(args.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
        {
            Program.Main(["--check"]);
        }

        // *************************** APP BUILD *******************************

        int asyncHandlersCount = 0;
        int syncHandlersCount = 0;
        string memoryType = string.Empty;

        // Usual .NET web app builder with settings from appsettings.json, appsettings.<ENV>.json, and env vars
        WebApplicationBuilder appBuilder = WebApplication.CreateBuilder();

        // Add config files, user secretes, and env vars
        appBuilder.Configuration.AddKernelMemoryConfigurationSources();

        // Read KM settings, needed before building the app.
        KernelMemoryConfig config = appBuilder.Configuration.GetSection("KernelMemory").Get<KernelMemoryConfig>()
                                    ?? throw new ConfigurationException("Unable to load configuration");

        // Some OpenAPI Explorer/Swagger dependencies
        appBuilder.ConfigureSwagger(config);

        // Prepare memory builder, sharing the service collection used by the hosting service
        // Internally build the memory client and make it available for dependency injection
        appBuilder.AddKernelMemory(memoryBuilder =>
        {
            // Prepare the builder with settings from config files
            memoryBuilder.ConfigureDependencies(appBuilder.Configuration).WithoutDefaultHandlers();

            // When using distributed orchestration, handlers are hosted in the current app and need to be con
            asyncHandlersCount = AddHandlersAsHostedServices(config, memoryBuilder, appBuilder);
        },
            memory =>
            {
                // When using in process orchestration, handlers are hosted by the memory orchestrator
                syncHandlersCount = AddHandlersToServerlessMemory(config, memory);

                memoryType = ((memory is MemoryServerless) ? "Sync - " : "Async - ") + memory.GetType().FullName;
            },
            services =>
            {
                long? maxSize = config.Service.GetMaxUploadSizeInBytes();
                if (!maxSize.HasValue) { return; }

                services.Configure<IISServerOptions>(x => { x.MaxRequestBodySize = maxSize.Value; });
                services.Configure<KestrelServerOptions>(x => { x.Limits.MaxRequestBodySize = maxSize.Value; });
                services.Configure<FormOptions>(x =>
                {
                    x.MultipartBodyLengthLimit = maxSize.Value;
                    x.ValueLengthLimit = int.MaxValue;
                });
            });

        // CORS
        bool enableCORS = false;
        const string CORSPolicyName = "KM-CORS";
        if (enableCORS && config.Service.RunWebService)
        {
            appBuilder.Services.AddCors(options =>
            {
                options.AddPolicy(name: CORSPolicyName, policy =>
                {
                    policy
                        .WithMethods("HEAD", "GET", "POST", "PUT", "DELETE")
                        .WithExposedHeaders("Content-Type", "Content-Length", "Last-Modified");
                    // .AllowAnyOrigin()
                    // .WithOrigins(...)
                    // .AllowAnyHeader()
                    // .WithHeaders(...)
                });
            });
        }

        // Build .NET web app as usual
        WebApplication app = appBuilder.Build();

        if (config.Service.RunWebService)
        {
            if (enableCORS) { app.UseCors(CORSPolicyName); }

            app.UseSwagger(config);
            var errorFilter = new HttpErrorsEndpointFilter();
            var authFilter = new HttpAuthEndpointFilter(config.ServiceAuthorization);
            app.MapGet("/", () => Results.Ok("Ingestion service is running. " +
                                             "Uptime: " + (DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                                                           - s_start.ToUnixTimeSeconds()) + " secs " +
                                             $"- Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}"))
                .AddEndpointFilter(errorFilter)
                .AddEndpointFilter(authFilter)
                .WithName("ServiceStatus")
                .WithDisplayName("ServiceStatus")
                .WithDescription("Show the service status and uptime.")
                .WithSummary("Show the service status and uptime.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

            // Add HTTP endpoints using minimal API (https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
            app.AddKernelMemoryEndpoints("/", config, [errorFilter, authFilter]);

            // Health probe
            app.MapGet("/health", () => Results.Ok("Service is running."))
                .WithName("ServiceHealth")
                .WithDisplayName("ServiceHealth")
                .WithDescription("Show if the service is healthy.")
                .WithSummary("Show if the service is healthy.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

            if (config.ServiceAuthorization.Enabled && config.ServiceAuthorization.AccessKey1 == config.ServiceAuthorization.AccessKey2)
            {
                app.Logger.LogError("KM Web Service: Access keys 1 and 2 have the same value. Keys should be different to allow rotation.");
            }
        }

        // *************************** START ***********************************

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.IsNullOrEmpty(env))
        {
            app.Logger.LogError("ASPNETCORE_ENVIRONMENT env var not defined.");
        }

        Console.WriteLine("***************************************************************************************************************************");
        Console.WriteLine("* Environment         : " + (string.IsNullOrEmpty(env) ? "WARNING: ASPNETCORE_ENVIRONMENT env var not defined" : env));
        Console.WriteLine("* Memory type         : " + memoryType);
        Console.WriteLine("* Pipeline handlers   : " + $"{syncHandlersCount} synchronous / {asyncHandlersCount} asynchronous");
        Console.WriteLine("* Web service         : " + (config.Service.RunWebService ? "Enabled" : "Disabled"));

        if (config.Service.RunWebService)
        {
            const double AspnetDefaultMaxUploadSize = 30000000d / 1024 / 1024;
            Console.WriteLine("* Web service auth    : " + (config.ServiceAuthorization.Enabled ? "Enabled" : "Disabled"));
            Console.WriteLine("* Max HTTP req size   : " + (config.Service.MaxUploadSizeMb ?? AspnetDefaultMaxUploadSize).ToString("0.#", CultureInfo.CurrentCulture) + " Mb");
            Console.WriteLine("* OpenAPI swagger     : " + (config.Service.OpenApiEnabled ? "Enabled" : "Disabled"));
        }

        Console.WriteLine("* Memory Db           : " + app.Services.GetService<IMemoryDb>()?.GetType().FullName);
        Console.WriteLine("* Document storage    : " + app.Services.GetService<IDocumentStorage>()?.GetType().FullName);
        Console.WriteLine("* Embedding generation: " + app.Services.GetService<ITextEmbeddingGenerator>()?.GetType().FullName);
        Console.WriteLine("* Text generation     : " + app.Services.GetService<ITextGenerator>()?.GetType().FullName);
        Console.WriteLine("* Content moderation  : " + app.Services.GetService<IContentModeration>()?.GetType().FullName);
        Console.WriteLine("* Log level           : " + app.Logger.GetLogLevelName());
        Console.WriteLine("***************************************************************************************************************************");

        app.Logger.LogInformation(
            "Starting Kernel Memory service, .NET Env: {EnvironmentType}, Log Level: {LogLevel}, Web service: {WebServiceEnabled}, Auth: {WebServiceAuthEnabled}, Pipeline handlers: {HandlersEnabled}",
            env,
            app.Logger.GetLogLevelName(),
            config.Service.RunWebService,
            config.ServiceAuthorization.Enabled,
            config.Service.RunHandlers);

        // Start web service and handler services
        try
        {
            app.Run();
        }
        catch (IOException e)
        {
            Console.WriteLine($"I/O error: {e.Message}");
            Environment.Exit(-1);
        }
    }

    /// <summary>
    /// Register handlers as asynchronous hosted services
    /// </summary>
    private static int AddHandlersAsHostedServices(
        KernelMemoryConfig config,
        IKernelMemoryBuilder memoryBuilder,
        WebApplicationBuilder appBuilder)
    {
        if (!string.Equals(config.DataIngestion.OrchestrationType, KernelMemoryConfig.OrchestrationTypeDistributed, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (!config.Service.RunHandlers) { return 0; }

        // Handlers are enabled via configuration in appsettings.json and/or appsettings.<env>.json
        memoryBuilder.WithoutDefaultHandlers();

        // Register all pipeline handlers defined in the configuration to run as hosted services
        foreach (KeyValuePair<string, HandlerConfig> handlerConfig in config.Service.Handlers)
        {
            appBuilder.Services.AddHandlerAsHostedService(config: handlerConfig.Value, stepName: handlerConfig.Key);
        }

        // Return registered handlers count
        return appBuilder.Services.Count(s => typeof(IPipelineStepHandler).IsAssignableFrom(s.ServiceType));
    }

    /// <summary>
    /// Register handlers instances inside the synchronous orchestrator
    /// </summary>
    private static int AddHandlersToServerlessMemory(
        KernelMemoryConfig config, IKernelMemory memory)
    {
        if (memory is not MemoryServerless) { return 0; }

        var orchestrator = ((MemoryServerless)memory).Orchestrator;
        foreach (KeyValuePair<string, HandlerConfig> handlerConfig in config.Service.Handlers)
        {
            orchestrator.AddSynchronousHandler(handlerConfig.Value, handlerConfig.Key);
        }

        return orchestrator.HandlerNames.Count;
    }
}