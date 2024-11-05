using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;
using System.Net;
using System.Text;

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion("gpt-4o", OpenAICredential.EndPoint, OpenAICredential.ApiKey);

var kernel = builder.Build();
var chatService = kernel.Services.GetService<IChatCompletionService>();

var chatHistory = new ChatHistory();

#region 4. 시스템 메시지

var systemMessage = @"너는 사용자에게 날씨를 안내하는 '날씨 요정'이야.
따뜻하고 친절한 말투로 안내를 해줘. 옷차림도 추천해주면 좋아.";
chatHistory.AddSystemMessage(systemMessage);

#endregion

#region 5. 날씨 조회 Function Calling

OpenAIPromptExecutionSettings settings = new() { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };

kernel.CreateFunctionFromMethod(async ([Description("Latitude of the location")] string latitude, [Description("Longitude of the location")] string longitude) =>
{
    var sb = new StringBuilder();

    var httpClient = new HttpClient();
    sb.AppendLine("[Weather]");

    var uri = $"http://api.openweathermap.org/data/2.5/forecast?lat={latitude}&lon={longitude}&appid={OpenAICredential.OpenWeatherAppId}&units=metric";
    var weatherResult = await httpClient.GetStringAsync(uri);
    sb.AppendLine(weatherResult);
    sb.AppendLine("[Air]");

    uri = $"http://api.openweathermap.org/data/2.5/air_pollution?lat={latitude}&lon={longitude}&appid={OpenAICredential.OpenWeatherAppId}";
    var airResult = await httpClient.GetStringAsync(uri);
    sb.AppendLine(airResult);

    return weatherResult + airResult;
}, "GetWeather", "Retrieve weather information from OpenWeather.");


#endregion

#region 2. Input

while (true)
{
    Console.Write("User : ");
    var input = Console.ReadLine();
    await Input(input);

}

async Task Input(string input)
{
    chatHistory.AddUserMessage(input);

    #region 3. Streaming

    var result = chatService.GetStreamingChatMessageContentsAsync(chatHistory, settings, kernel);

    Console.WriteLine();
    Console.Write("Assistant : ");

    var assistantMsg = string.Empty;
    await foreach (var text in result)
    {
        await Task.Delay(20);
        assistantMsg += text;
        Console.Write(text);
    }

    Console.WriteLine();
    Console.WriteLine();

    #endregion
}

#endregion
