using demo01;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;


#region Configuration
IConfiguration config = new ConfigurationBuilder()
                                .AddUserSecrets<Program>()
                                .Build();

string deployName = config["AOAI:DeployName"];
string endPoint = config["AOAI:EndPoint"];
string key = config["AOAI:Key"];
string model = config["AOAI:Model"];
#endregion

/*
 * 1. 필터란?
 * 2. 필터의 종류
 *      - PromptRenderFilter
 *      - FunctionInvocationFilter
 *      - AutoFunction Invocation Filter
 * 3. Demo
 */

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(deployName, endPoint, key, model);

builder.Services.AddSingleton<IFunctionInvocationFilter, FunctionFilter1>();
builder.Services.AddSingleton<IPromptRenderFilter, PromptFilter1>();
builder.Services.AddSingleton<IPromptRenderFilter, PromptFilter2>();
builder.Services.AddSingleton<IFunctionInvocationFilter, FunctionFilter2>();

//  각 속성에 따로 넣으셔도 됩니다.
//  kernel.PromptRenderFilters.Add(new PromptFilter1());
//  kernel.PromptRenderFilters.Add(new PromptFilter2());
//  kernel.FunctionInvocationFilters.Add(new FunctionFilter1());
//  kernel.FunctionInvocationFilters.Add(new FunctionFilter2());

var kernel = builder.Build();

var response = await kernel.InvokePromptAsync("Explain Semantic Kernel filtering.");
Console.WriteLine("\n응답:");
Console.WriteLine(response);
Console.WriteLine("\n");
