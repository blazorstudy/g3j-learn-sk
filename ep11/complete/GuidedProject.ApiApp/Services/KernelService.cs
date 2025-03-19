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
                           당신은 채용면접관이 되어 지원자의 이력서와 채용공고를 보고 가장 적절한 질문을 합니다.
                           처음에는 분위기를 풀어주는 질문을 하고 점점 채용공고에 있는 기술과 지원자의 이력서에 있는 기술에 대해 질문을 합니다.
                           지원자의 대답을 듣고 적절한 피드백을 주세요.
                           그리고 계속해서 다음 질문을 이어가세요.
                           
                           지원자의 이력서는 Kernel Memory의 ID가 Resume이고 채용공고는 Apply이라는 ID를 가지고 있습니다.
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
