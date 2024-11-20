#pragma warning disable SKEXP0050
using Microsoft.SemanticKernel;

var yourDeploymentName = "";
var yourEndpoint = "";
var yourApiKey = "";

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    deploymentName: yourDeploymentName,
    endpoint: yourEndpoint,
    apiKey: yourApiKey);

var kernel = builder.Build();
var result = await kernel.InvokePromptAsync(
    "Give me a list of breakfast foods with eggs and cheese");

Console.WriteLine(result);