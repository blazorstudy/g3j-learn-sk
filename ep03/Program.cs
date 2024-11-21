#pragma warning disable SKEXP0050
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        deploymentName: config["deploymentName"] ?? string.Empty,
        endpoint: config["endpoint"] ?? string.Empty,
        apiKey: config["apiKey"] ?? string.Empty)
    .Build();

var plugins = kernel.CreatePluginFromPromptDirectory("Prompts");

var history = @"""
    저는 매운 음식을 아주아주 좋아합니다.
    한식, 일식, 중식 등 아시아권 음식 또한 좋아합니다.
    그런데 어제는 짜장면, 잡채밥, 돼지국밥, 회초밥을 먹었고
    오늘은 쌀국수랑 돈까스 나베를 먹었습니다.
    """;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("어떤 음식 종류로 추천해드릴까요?");

var category = Console.ReadLine();

var result = await kernel
    .InvokeAsync(
        plugins["RecommendFood"],
        new()
        {
            { "history", history },
            { "foodCategory", category }
        })
    .ConfigureAwait(false);

Console.WriteLine(result);