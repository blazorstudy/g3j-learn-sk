using System.Globalization;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Service.AspNetCore;

using WebKernelMemoryService.Internals;

namespace WebKernelMemoryService;

public static class WebMainProgram
{
    public static void Main(string[] args)
    {
        SensitiveDataLogger.Enabled = true;
        SensitiveDataLogger.LoggingLevel = LogLevel.Trace;

        string memoryType = string.Empty;

        // Usual .NET web app builder with settings from appsettings.json, appsettings.<ENV>.json, and env vars
        WebApplicationBuilder appBuilder = WebApplication.CreateBuilder();

        // Add config files, user secretes, and env vars
        appBuilder.Configuration.AddKernelMemoryConfigurationSources();

        // Read KM settings, needed before building the app.
        KernelMemoryConfig config = appBuilder.Configuration
                                              .GetSection("KernelMemory")
                                              .Get<KernelMemoryConfig>()
                                 ?? throw new ConfigurationException("Unable to load configuration");

        // Some OpenAPI Explorer/Swagger dependencies
        appBuilder.ConfigureSwagger(config);

        // Prepare memory builder, sharing the service collection used by the hosting service
        // Internally build the memory client and make it available for dependency injection
        appBuilder.AddKernelMemory(memoryBuilder =>
            {
                // Prepare the builder with settings from config files
                memoryBuilder.ConfigureDependencies(appBuilder.Configuration).WithoutDefaultHandlers();

                if (string.Equals(config.DataIngestion.OrchestrationType, KernelMemoryConfig.OrchestrationTypeDistributed, StringComparison.OrdinalIgnoreCase) &&
                    config.Service.RunHandlers)
                {
                    // Handlers are enabled via configuration in appsettings.json and/or appsettings.<env>.json
                    memoryBuilder.WithoutDefaultHandlers();

                    // Register all pipeline handlers defined in the configuration to run as hosted services
                    foreach (KeyValuePair<string, HandlerConfig> handlerConfig in config.Service.Handlers)
                    {
                        appBuilder.Services.AddHandlerAsHostedService(config: handlerConfig.Value, stepName: handlerConfig.Key);
                    }
                }
            },
            memory =>
            {
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

        // Build .NET web app as usual
        WebApplication app = appBuilder.Build();

        if (config.Service.RunWebService)
        {
            app.UseSwagger(config);

            // Add HTTP endpoints using minimal API (https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
            app.AddKernelMemoryEndpoints("/", config);
        }

        // *************************** START ***********************************

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        Console.WriteLine("***************************************************************************************************************************");
        Console.WriteLine("* Environment         : " + (string.IsNullOrEmpty(env) ? "WARNING: ASPNETCORE_ENVIRONMENT env var not defined" : env));
        Console.WriteLine("* Memory type         : " + memoryType);
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
            $"Starting Kernel Memory service, " +
            $".NET Env: {env}, " +
            $"Log Level: {app.Logger.GetLogLevelName()}, " +
            $"Web service: {config.Service.RunWebService}, " +
            $"Auth: {config.ServiceAuthorization.Enabled}, " +
            $"Pipeline handlers: {config.Service.RunHandlers}");

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
}