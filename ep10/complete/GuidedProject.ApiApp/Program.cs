using System.ClientModel;

using GuidedProject.ApiApp.Endpoints;
using GuidedProject.ApiApp.Services;

using Microsoft.SemanticKernel;

using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddScoped<IKernelService, KernelService>();

builder.Services.AddSingleton<Kernel>(sp =>
{
    var config = builder.Configuration;

    var client = new OpenAIClient(
        credential: new ApiKeyCredential(config["GitHub:Models:AccessToken"]!),
        options: new OpenAIClientOptions { Endpoint = new Uri(config["GitHub:Models:Endpoint"]!) });

    var kernel = Kernel.CreateBuilder()
                       .AddOpenAIChatCompletion(
                           modelId: config["GitHub:Models:ModelId"]!,
                           openAIClient: client,
                           serviceId: "github")
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
