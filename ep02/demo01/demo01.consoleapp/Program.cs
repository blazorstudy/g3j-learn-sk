using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;

// Get Azure OpenAI access details
var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();

// Build Semantic Kernel
var kernel = Kernel.CreateBuilder()
                   .AddAzureOpenAIChatCompletion(
                       endpoint: config["OpenAI:Endpoint"]!,
                       apiKey: config["OpenAI:ApiKey"]!,
                       deploymentName: config["OpenAI:DeploymentName"]!)
                   .Build();

// Get prompt from user
Console.WriteLine("User:");
var prompt = Console.ReadLine();

// Run prompt directly from the kernel
var result = await kernel.InvokePromptAsync(prompt!);
Console.WriteLine("Assistant:");
Console.WriteLine(result!.GetValue<string>());
