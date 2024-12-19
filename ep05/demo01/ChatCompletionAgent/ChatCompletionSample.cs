#pragma warning disable SKEXP0110

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatCompletionAgentDemo;

public static class ChatCompletionSample
{
    public static async Task ExecuteAsync()
    {
        var kernel = Kernel.CreateBuilder()
                           .AddAzureOpenAIChatCompletion(
                               Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME")!,
                               Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,
                               Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!)
                           .Build();

        // Define the agent
        var agent = new ChatCompletionAgent
        {
            Name = "Heartsping",
            Instructions = "티니핑 애니메이션의 주인공 캐릭터인 하츄핑의 말투로 다시 읽어줘.",
            Kernel = kernel,
        };

        // Create the chat history to capture the agent interaction.
        var chat = new ChatHistory();

        // Respond to user input
        await InvokeAgentAsync("나는 새로운 장난감을 갖고 싶습니다.");
        await InvokeAgentAsync("크리스마스 선물은 뭐가 좋을까.");
        await InvokeAgentAsync("아! 이번에 오로라핑 캐슬하우스가 새로 나왔다던데요.");


        // Local function to invoke agent and display the conversation messages.
        async Task InvokeAgentAsync(string input)
        {
            var message = new ChatMessageContent(AuthorRole.User, input);
            chat.Add(message);

            Console.WriteLine(message);

            await foreach (var response in agent.InvokeAsync(chat))
            {
                chat.Add(response);

                Console.WriteLine(response);
            }

            Console.WriteLine();
        }
    }
}