using System.ClientModel;

using GuidedProject.ApiApp.Endpoints;
using GuidedProject.ApiApp.Services;

using Microsoft.SemanticKernel;

using OllamaSharp;

using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddScoped<IKernelService, KernelService>();

builder.AddAzureOpenAIClient("openai");
builder.AddOllamaApiClient("ollama-phi4");

builder.Services.AddSingleton<Kernel>(sp =>
{
    var config = builder.Configuration;

    var openAIClient = sp.GetRequiredService<OpenAIClient>();
    var ollamaClient = sp.GetRequiredService<IOllamaApiClient>();
    // var client = new OpenAIClient(
    //     credential: new ApiKeyCredential(config["GitHub:Models:AccessToken"]!),
    //     options: new OpenAIClientOptions { Endpoint = new Uri(config["GitHub:Models:Endpoint"]!) });

    var kernel = Kernel.CreateBuilder()
                       .AddOpenAIChatCompletion(
                           modelId: config["GitHub:Models:ModelId"]!,
                           openAIClient: openAIClient,
                           serviceId: "github")
                       .AddOllamaChatCompletion(
                           ollamaClient: (OllamaApiClient)ollamaClient,
                           serviceId: "ollama")
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

app.UseHttpsRedirection();

app.MapChatCompletionEndpoint();

app.Run();
