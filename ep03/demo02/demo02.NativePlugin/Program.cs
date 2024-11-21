// See https://aka.ms/new-console-template for more information
using demo02.NativePlugin;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

IConfiguration config = new ConfigurationBuilder()
                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                .AddUserSecrets<Program>()
                                .Build();

string deployName = config["AOAI:DeployName"];
string endPoint = config["AOAI:EndPoint"];
string key = config["AOAI:Key"];
string model = config["AOAI:Model"];

var kernel = Kernel.CreateBuilder()
                    //.AddAzureOpenAIChatCompletion(deployName, endPoint, key, model)
                    .Build();

var gamePlugin = kernel.ImportPluginFromType<GamePlugin>("Game");

//var result0 = await kernel.InvokePromptAsync("Semantic Kernel에서 Plugin을 설명해");
//Console.WriteLine($"{result0}");

var result1 = await kernel.InvokeAsync(gamePlugin["roll_dice"]);
Console.WriteLine($"dice : {result1}");

var result2 = await kernel.InvokeAsync(gamePlugin["flip_coin"]);
Console.WriteLine($"coin : {result2}");

