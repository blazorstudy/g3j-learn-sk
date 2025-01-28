using System.ClientModel;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

using OpenAI;

using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using TextSearchWithVectorStore.ConsoleApp;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var config = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                 .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                 .Build();

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
//                              .AddConsoleExporter()
//                              .AddOtlpExporter(options => options.Endpoint = new Uri(dashboardEndpoint))
//                              .Build();

// using var meterProvider = Sdk.CreateMeterProviderBuilder()
//                              .SetResourceBuilder(resourceBuilder)
//                              .AddMeter("Microsoft.SemanticKernel*")
//                              .AddConsoleExporter()
//                              .AddOtlpExporter(options => options.Endpoint = new Uri(dashboardEndpoint))
//                              .Build();

// using var loggerFactory = LoggerFactory.Create(builder =>
// {
//     // Add OpenTelemetry as a logging provider
//     builder.AddOpenTelemetry(options =>
//     {
//         options.SetResourceBuilder(resourceBuilder);
//         options.AddConsoleExporter();
//         options.AddOtlpExporter(options => options.Endpoint = new Uri(dashboardEndpoint));
//         // Format log messages. This is default to false.
//         options.IncludeFormattedMessage = true;
//         options.IncludeScopes = true;
//     });
//     builder.SetMinimumLevel(LogLevel.Information);
// });
#endregion

#region Semantic Kernel Text Search with Vector Store
var service = new TextSearchService(config);
var collection = await service.GetVectorStoreRecordCollectionAsync("records");
var search = await service.GetVectorStoreTextSearchAsync(collection);

var query = "What is the Semantic Kernel?";

var searchResults = await search.GetTextSearchResultsAsync(query, new TextSearchOptions() { Top = 2, Skip = 0 });

Console.WriteLine("\n--- Text Search Results ---\n");
await foreach (var result in searchResults.Results)
{
    Console.WriteLine($"Name:  {result.Name}");
    Console.WriteLine($"Value: {result.Value}");
    Console.WriteLine($"Link:  {result.Link}");
}
#endregion

#region Semantic Kernel Text Search from Chat Completions
// var openAIClient = new OpenAIClient(new ApiKeyCredential(config["GitHub:Models:0:Token"]!), new OpenAIClientOptions { Endpoint = new Uri(config["GitHub:Models:0:Endpoint"]!) });

// var builder = Kernel.CreateBuilder();

// // Add logger
// // builder.Services.AddSingleton(loggerFactory);

// builder.AddOpenAIChatCompletion(config["GitHub:Models:0:ModelId"]!, openAIClient);
// var kernel = builder.Build();

// var plugin = search.CreateWithGetTextSearchResults("SearchPlugin");
// kernel.Plugins.Add(plugin);

// var promptTemplate = """
//     {{#with (SearchPlugin-GetTextSearchResults query)}}  
//         {{#each this}}  
//         Name: {{Name}}
//         Value: {{Value}}
//         Link: {{Link}}
//         -----------------
//         {{/each}}  
//     {{/with}}  

//     {{query}}

//     Include citations to the relevant information where it is referenced in the response.
//     """;

// var arguments = new KernelArguments() { { "query", query } };

// var promptResult = await kernel.InvokePromptAsync(
//     promptTemplate: promptTemplate,
//     arguments: arguments,
//     templateFormat: HandlebarsPromptTemplateFactory.HandlebarsTemplateFormat,
//     promptTemplateFactory: new HandlebarsPromptTemplateFactory());

// Console.WriteLine("\n--- Text Search Results from Chat Completions ---\n");
// Console.WriteLine(promptResult);
#endregion

#region Semantic Kernel Text Search from Chat Completions with Auto Function Calling
// var settings = new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
// arguments = new KernelArguments(settings);

// var functionCalingResult = await kernel.InvokePromptAsync(
//     promptTemplate: query,
//     arguments: arguments);

// Console.WriteLine("\n--- Text Search Results from Chat Completions with Auto Function Calling ---\n");
// Console.WriteLine(functionCalingResult);
#endregion

#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
