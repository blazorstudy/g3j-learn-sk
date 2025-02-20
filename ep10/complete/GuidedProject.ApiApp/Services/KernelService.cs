using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using OpenAI.Chat;

using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace GuidedProject.ApiApp.Services;

public interface IKernelService
{
    IAsyncEnumerable<string> CompleteChatStreamingAsync(string prompt);
    IAsyncEnumerable<string> CompleteChatStreamingAsync(IEnumerable<ChatMessageContent> messages);
}

public class KernelService(Kernel kernel, IConfiguration config) : IKernelService
{
    public async IAsyncEnumerable<string> CompleteChatStreamingAsync(string prompt)
    {
        var settings = new PromptExecutionSettings { ServiceId = config["SemanticKernel:ServiceId"] };
        var arguments = new KernelArguments(settings);

        var result = kernel.InvokePromptStreamingAsync(prompt, arguments).ConfigureAwait(false);

        await foreach (var text in result)
        {
            yield return text.ToString();
        }
    }

    public async IAsyncEnumerable<string> CompleteChatStreamingAsync(IEnumerable<ChatMessageContent> messages)
    {
        var history = new ChatHistory();
        history.AddRange(messages);

        var service = kernel.GetRequiredService<IChatCompletionService>();
        var settings = new PromptExecutionSettings { ServiceId = config["SemanticKernel:ServiceId"] };

        var result = service.GetStreamingChatMessageContentsAsync(history, settings, kernel);
        await foreach (var text in result)
        {
            yield return text.ToString();
        }
    }
}
