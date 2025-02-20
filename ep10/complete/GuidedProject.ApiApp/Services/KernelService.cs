using Microsoft.SemanticKernel;

namespace GuidedProject.ApiApp.Services;

public interface IKernelService
{
    IAsyncEnumerable<string> CompleteChatStreamingAsync(string prompt);
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
}
