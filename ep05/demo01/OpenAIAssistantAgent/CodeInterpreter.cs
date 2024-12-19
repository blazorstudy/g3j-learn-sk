#pragma warning disable SKEXP0110
#pragma warning disable OPENAI001

using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel;
using System.ClientModel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace OpenAIAssistantAgentDemo;

public static class CodeInterpreter
{
    public static async Task ExecuteAsync()
    {
        var provider = OpenAIClientProvider.ForAzureOpenAI(
            new ApiKeyCredential(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!),
            new Uri(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!));

        var agent = await OpenAIAssistantAgent.CreateAsync(
            clientProvider: provider,
            definition: new(Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME")!)
            {
                EnableCodeInterpreter = true
            },
            kernel: new Kernel());

        // Create a thread for the agent conversation.
        var threadId = await agent.CreateThreadAsync();

        // Respond to user input
        try
        {
            await InvokeAgentAsync("코드를 사용하여 피보나치 수열에서 101의 값보다 작은 값을 찾는 방법?");
        }
        finally
        {
            await agent.DeleteThreadAsync(threadId);
            await agent.DeleteAsync();
        }
        // Local function to invoke agent and display the conversation messages.
        async Task InvokeAgentAsync(string input)
        {
            var message = new ChatMessageContent(AuthorRole.User, input);
            await agent.AddChatMessageAsync(threadId, message);


            Console.WriteLine(message);

            await foreach (ChatMessageContent response in agent.InvokeAsync(threadId))
            {
                Console.WriteLine(response);
            }

            Console.WriteLine();
        }
    }
}