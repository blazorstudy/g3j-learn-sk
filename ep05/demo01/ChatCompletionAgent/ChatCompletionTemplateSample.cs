#pragma warning disable SKEXP0110

using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;

namespace ChatCompletionAgentDemo;

public static class ChatCompletionTemplateSample
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
        string generateStoryYaml = await File.ReadAllTextAsync("Resources/GenerateStory.yaml");
        PromptTemplateConfig templateConfig = KernelFunctionYaml.ToPromptTemplateConfig(generateStoryYaml);

        // Instructions, Name and Description properties defined via the config.
        ChatCompletionAgent agent =
            new(templateConfig, new KernelPromptTemplateFactory())
            {
                Kernel = kernel,
                Arguments = new KernelArguments
                {
                    { "topic", "하츄핑" },
                    { "length", "3" },
                }
            };

        // Create the chat history to capture the agent interaction.
        ChatHistory chat = [];

        // Invoke the agent with the default arguments.
        await InvokeAgentAsync();

        // Invoke the agent with the override arguments.
        await InvokeAgentAsync(
            new()
            {
                { "topic", "Cat" },
                { "length", "3" },
            });

        // Local function to invoke agent and display the conversation messages.
        async Task InvokeAgentAsync(KernelArguments? arguments = null)
        {
            await foreach (ChatMessageContent content in agent.InvokeAsync(chat, arguments))
            {
                chat.Add(content);

                Console.WriteLine(content);
            }

            Console.WriteLine();
        }
    }
}