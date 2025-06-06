using GuidedProject.WebApp.Models;

namespace GuidedProject.WebApp.ApiClients;

public interface IChatClient
{
    IAsyncEnumerable<string> CompleteChatStreamingAsync(string prompt);
}

public class ChatClient(HttpClient http) : IChatClient
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
}
