using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace demo01;

public class Sample
{
    public static async Task showOverview(Kernel kernel)
    {
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        ChatHistory history = [];
        history.AddUserMessage("자기소개 부탁해");

        // 비스트리밍 
        var response = await chatService.GetChatMessageContentAsync(
            history,
            kernel: kernel
        );
        Console.WriteLine(response);
    }

    public static async Task showChatHistory(Kernel kernel)
    {
        // 조금 더 리치한 거
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        ChatHistory history = [];
        history.AddUserMessage("내가 좋아하는 스포츠는 축구야");
        history.AddAssistantMessage("축구를 좋아하시는군요. 어느 팀을 좋아하시나요?");
        history.Add(
                    new()
                    {
                        Role = AuthorRole.User,
                        Items = [
                            new TextContent { Text = "한국팀을 좋아해" },
                            new TextContent { Text = "월드컵은 언제 열려?" }
                        ]
                    });

        // 스트리밍 챗 컴플리션
        var response = chatService.GetStreamingChatMessageContentsAsync(history, kernel: kernel);
        await foreach (var chuck in response)
        {
            Console.WriteLine(chuck);
        }
    }

    public static async Task showPersonas(Kernel kernel)
    {
        // 시스템 메시지 세팅
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        ChatHistory history = [];
        history.AddSystemMessage("당신은 한국인이고 남성입니다. 프로그래머이고 취미는 게임하기 입니다. 답변은 단답형으로 합니다.");
        history.AddUserMessage("자기소개 부탁해");

        // 비스트리밍 
        var response = await chatService.GetChatMessageContentAsync(
            history,
            kernel: kernel
        );
        Console.WriteLine(response);
    }
}
