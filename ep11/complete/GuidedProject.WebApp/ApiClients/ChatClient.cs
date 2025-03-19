using GuidedProject.WebApp.Models;

using Microsoft.KernelMemory;

namespace GuidedProject.WebApp.ApiClients;

public interface IChatClient
{
    IAsyncEnumerable<string> CompleteChatStreamingAsync(string prompt);
    IAsyncEnumerable<string> CompleteChatStreamingWithHistoryAsync(IEnumerable<ChatMessage> messages);

    Task<string> UpdateMemoryFromWeb(string key, string uri);
    Task<string> UpdateMemoryFromText(string key, string text);
}

public class ChatClient(HttpClient http, MemoryWebClient memoryWebClient) : IChatClient
{
    private const string REQUEST_URI = "api/chat/complete";

    public async IAsyncEnumerable<string> CompleteChatStreamingAsync(string prompt)
    {
        var content = new PromptRequest(prompt);
        var response = await http.PostAsJsonAsync<PromptRequest>(REQUEST_URI, content);

        response.EnsureSuccessStatusCode();

        var result = response.Content.ReadFromJsonAsAsyncEnumerable<PromptResponse>();
        await foreach (var message in result)
        {
            yield return message!.Content;
        }
    }

    public async IAsyncEnumerable<string> CompleteChatStreamingWithHistoryAsync(IEnumerable<ChatMessage> messages)
    {
        var content = messages.Select(p => new PromptWithRoleRequest(p.Role, p.Content));
        var response = await http.PostAsJsonAsync<IEnumerable<PromptWithRoleRequest>>($"{REQUEST_URI}-with-role", content);

        response.EnsureSuccessStatusCode();

        var result = response.Content.ReadFromJsonAsAsyncEnumerable<PromptResponse>();
        await foreach (var message in result)
        {
            yield return message!.Content;
        }
    }

    public async Task<string> UpdateMemoryFromWeb(string key, string uri)
    {
        var id = await memoryWebClient.ImportWebPageAsync(uri, key);

        while (!await memoryWebClient.IsDocumentReadyAsync(id))
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return id;
    }

    public async Task<string> UpdateMemoryFromText(string key, string text)
    {
        var id = await memoryWebClient.ImportTextAsync(text, key);

        while (!await memoryWebClient.IsDocumentReadyAsync(id))
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return id;
    }
}
