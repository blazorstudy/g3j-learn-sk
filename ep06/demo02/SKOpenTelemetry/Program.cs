using Azure.Monitor.OpenTelemetry.Exporter;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using SKOpenTelemetry.Plugins;

var config = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                 .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                 .Build();

#region TELEMETRY SETUP for Console App
// var resourceBuilder = ResourceBuilder.CreateDefault()
//                                      .AddService("SKOpenTelemetry");

// // Enable model diagnostics with sensitive data.
// AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

// using var traceProvider = Sdk.CreateTracerProviderBuilder()
//                              .SetResourceBuilder(resourceBuilder)
//                              .AddSource("Microsoft.SemanticKernel*")
//                              .AddConsoleExporter()
//                              .Build();

// using var meterProvider = Sdk.CreateMeterProviderBuilder()
//                              .SetResourceBuilder(resourceBuilder)
//                              .AddMeter("Microsoft.SemanticKernel*")
//                              .AddConsoleExporter()
//                              .Build();

// using var loggerFactory = LoggerFactory.Create(builder =>
// {
//     // Add OpenTelemetry as a logging provider
//     builder.AddOpenTelemetry(options =>
//     {
//         options.SetResourceBuilder(resourceBuilder);
//         options.AddConsoleExporter();
//         // Format log messages. This is default to false.
//         options.IncludeFormattedMessage = true;
//         options.IncludeScopes = true;
//     });
//     builder.SetMinimumLevel(LogLevel.Information);
// });
#endregion

#region TELEMETRY SETUP for Console App with Aspire Dashboard
// Make sure to run the Aspire Dashboard locally using container:
// docker run --rm -it -d -p 18888:18888 -p 4317:18889 --name aspire-dashboard mcr.microsoft.com/dotnet/aspire-dashboard:9.0

// var dashboardEndpoint = config["Aspire:Dashboard:Endpoint"]!;
// var resourceBuilder = ResourceBuilder.CreateDefault()
//                                      .AddService("SKOpenTelemetry");

// // Enable model diagnostics with sensitive data.
// AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

// using var traceProvider = Sdk.CreateTracerProviderBuilder()
//                              .SetResourceBuilder(resourceBuilder)
//                              .AddSource("Microsoft.SemanticKernel*")
//                              .AddOtlpExporter(options => options.Endpoint = new Uri(dashboardEndpoint))
//                              .Build();

// using var meterProvider = Sdk.CreateMeterProviderBuilder()
//                              .SetResourceBuilder(resourceBuilder)
//                              .AddMeter("Microsoft.SemanticKernel*")
//                              .AddOtlpExporter(options => options.Endpoint = new Uri(dashboardEndpoint))
//                              .Build();

// using var loggerFactory = LoggerFactory.Create(builder =>
// {
//     // Add OpenTelemetry as a logging provider
//     builder.AddOpenTelemetry(options =>
//     {
//         options.SetResourceBuilder(resourceBuilder);
//         options.AddOtlpExporter(options => options.Endpoint = new Uri(dashboardEndpoint));
//         // Format log messages. This is default to false.
//         options.IncludeFormattedMessage = true;
//         options.IncludeScopes = true;
//     });
//     builder.SetMinimumLevel(LogLevel.Information);
// });
#endregion

#region TELEMETRY SETUP for Console App with Application Insights
// var connectionString = config["Azure:ApplicationInsights:ConnectionString"]!;
// var resourceBuilder = ResourceBuilder.CreateDefault()
//                                      .AddService("SKOpenTelemetry");

// // Enable model diagnostics with sensitive data.
// AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

// using var traceProvider = Sdk.CreateTracerProviderBuilder()
//                              .SetResourceBuilder(resourceBuilder)
//                              .AddSource("Microsoft.SemanticKernel*")
//                              .AddAzureMonitorTraceExporter(options => options.ConnectionString = connectionString)
//                              .Build();

// using var meterProvider = Sdk.CreateMeterProviderBuilder()
//                              .SetResourceBuilder(resourceBuilder)
//                              .AddMeter("Microsoft.SemanticKernel*")
//                              .AddAzureMonitorMetricExporter(options => options.ConnectionString = connectionString)
//                              .Build();

// using var loggerFactory = LoggerFactory.Create(builder =>
// {
//     // Add OpenTelemetry as a logging provider
//     builder.AddOpenTelemetry(options =>
//     {
//         options.SetResourceBuilder(resourceBuilder);
//         options.AddAzureMonitorLogExporter(options => options.ConnectionString = connectionString);
//         // Format log messages. This is default to false.
//         options.IncludeFormattedMessage = true;
//         options.IncludeScopes = true;
//     });
//     builder.SetMinimumLevel(LogLevel.Information);
// });
#endregion

var builder = Kernel.CreateBuilder();

#region TELEMETRY SETUP
// builder.Services.AddSingleton(loggerFactory);
#endregion

builder.AddAzureOpenAIChatCompletion(
           deploymentName: config["Azure:OpenAI:DeploymentName"]!,
           endpoint: config["Azure:OpenAI:Endpoint"]!,
           apiKey: config["Azure:OpenAI:ApiKey"]!
       );

#region FUNCTION CALLING
// builder.Plugins.AddFromType<BookingPlugin>();
#endregion

var kernel = builder.Build();

while (true)
{
    Console.Write("User: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        break;
    }

    Console.Write("Assistant: ");
    var message = default(string);

#region PROMPT STREAMING
    var result = kernel.InvokePromptStreamingAsync(input);
#endregion

#region FUNCTION CALLING
    // var result = kernel.InvokePromptStreamingAsync(
    //     input,
    //     new KernelArguments(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
    // );
#endregion
    await foreach (var text in result)
    {
        await Task.Delay(20);
        message += text;
        Console.Write(text);
    }
    Console.WriteLine();
    Console.WriteLine();
}
