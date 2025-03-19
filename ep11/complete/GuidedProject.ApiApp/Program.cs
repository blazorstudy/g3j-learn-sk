#pragma warning disable KMEXP05 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using GuidedProject.ApiApp.Endpoints;
using GuidedProject.ApiApp.Services;

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.Service.AspNetCore;
using Microsoft.SemanticKernel;

using OllamaSharp;

using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.Configuration.AddKernelMemoryConfigurationSources();

builder.Services.AddOpenApi();

builder.Services.AddScoped<IKernelService, KernelService>();

KernelMemoryConfig config = builder.Configuration.GetSection("KernelMemory").Get<KernelMemoryConfig>();

builder.AddKernelMemory(x =>
{
    x.ConfigureDependencies(builder.Configuration).WithoutDefaultHandlers();

    foreach (KeyValuePair<string, HandlerConfig> handlerConfig in config.Service.Handlers)
    {
        builder.Services.AddHandlerAsHostedService(config: handlerConfig.Value, stepName: handlerConfig.Key);
    }
});

builder.AddAzureOpenAIClient("openai");
builder.AddKeyedOllamaApiClient("ollama-phi4-mini");
builder.AddKeyedOllamaApiClient("exaone");

builder.Services.AddSingleton<Kernel>(sp =>
{
    var config = builder.Configuration;

    var openAIClient = sp.GetRequiredService<OpenAIClient>();
    var ollamaClient = sp.GetRequiredKeyedService<IOllamaApiClient>("ollama-phi4-mini");
    var hfaceClient = sp.GetRequiredKeyedService<IOllamaApiClient>("exaone");

    var kernel = Kernel.CreateBuilder()
                       .AddOpenAIChatCompletion(
                           modelId: config["GitHub:Models:ModelId"]!,
                           openAIClient: openAIClient,
                           serviceId: "github")
                       .AddOllamaChatCompletion(
                           ollamaClient: (OllamaApiClient)ollamaClient,
                           serviceId: "ollama")
                       .AddOllamaChatCompletion(
                           ollamaClient: (OllamaApiClient)hfaceClient,
                           serviceId: "hface")
                       .Build();

    return kernel;
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.AddKernelMemoryEndpoints(kmConfig: config);

app.UseHttpsRedirection();

app.MapChatCompletionEndpoint();

app.Run();
