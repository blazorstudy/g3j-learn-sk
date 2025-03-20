using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace GuidedProject.ApiApp.Services;

public interface IKernelService
{
    IAsyncEnumerable<string> CompleteChatStreamingAsync(string prompt);
    IAsyncEnumerable<string> CompleteChatStreamingAsync(IEnumerable<ChatMessageContent> messages);
}

public class KernelService(Kernel kernel, IConfiguration config, IKernelMemory kernelMemory) : IKernelService
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
        // Chat setup
        var systemPrompt = """
                           ����� ä��������� �Ǿ� �������� �̷¼��� ä����� ���� ���� ������ ������ �մϴ�.
                           ó������ �����⸦ Ǯ���ִ� ������ �ϰ� ���� ä����� �ִ� ����� �������� �̷¼��� �ִ� ����� ���� ������ �մϴ�.
                           �������� ����� ��� ������ �ǵ���� �ּ���.
                           �׸��� ����ؼ� ���� ������ �̾����.
                           
                           �������� �̷¼��� Kernel Memory�� ID�� Resume�̰� ä������ Apply�̶�� ID�� ������ �ֽ��ϴ�.
                           """;

        var history = new ChatHistory();
        history.AddRange(messages);

        var service = kernel.GetRequiredService<IChatCompletionService>(config["SemanticKernel:ServiceId"]!);
        var query = $"{systemPrompt}\n{messages.LastOrDefault(x => !string.IsNullOrWhiteSpace(x.Content))?.Content}";

        var longTermMemory = await GetLongTermMemory(kernelMemory, query);

        // Inject the memory recall in the initial system message
        history[0].Content = $"{systemPrompt}\n\nLong term memory:\n{longTermMemory}";

        var result = service.GetStreamingChatMessageContentsAsync(chatHistory: history, kernel: kernel);
        await foreach (var text in result)
        {
            yield return text.ToString();
        }
    }

    private static async Task<string> GetLongTermMemory(IKernelMemory memory, string query, bool asChunks = true)
    {
        if (asChunks)
        {
            // Fetch raw chunks, using KM indexes. More tokens to process with the chat history, but only one LLM request.
            SearchResult memories = await memory.SearchAsync(query, limit: 10);
            return memories.Results.SelectMany(m => m.Partitions).Aggregate("", (sum, chunk) => sum + chunk.Text + "\n").Trim();
        }

        // Use KM to generate an answer. Fewer tokens, but one extra LLM request.
        MemoryAnswer answer = await memory.AskAsync(query);
        return answer.Result.Trim();
    }
}
