// See https://aka.ms/new-console-template for more information
using Microsoft.SemanticKernel.Plugins.OpenApi;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;


var config = new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .Build();
var service = config["services:apiservice:https:0"];
var kernel = Kernel.CreateBuilder()
                    .Build();

var plugin = await kernel.ImportPluginFromOpenApiAsync("apiPlugin", new Uri($"{service}/openapi/v1.json"));
var result = await kernel.InvokeAsync(plugin["FlipCoin"]);
Console.WriteLine($"FlipCoin : {result}");
Console.WriteLine("End...");