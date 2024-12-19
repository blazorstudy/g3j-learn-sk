#pragma warning disable SKEXP0110
#pragma warning disable OPENAI001

using Microsoft.SemanticKernel.Agents.OpenAI;
using OpenAI.Files;
using OpenAI.VectorStores;
using Microsoft.SemanticKernel.ChatCompletion;

using System.ClientModel;
using Microsoft.SemanticKernel;

namespace OpenAIAssistantAgentDemo;

public static class FileSearch
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
                EnableFileSearch = true
            },
            kernel: new Kernel());

        // Upload file - Using a table of fictional employees.
        var fileClient = provider.Client.GetOpenAIFileClient();
        await using Stream stream = File.OpenRead("Resources/employees.pdf")!;
        OpenAIFile fileInfo = await fileClient.UploadFileAsync(stream, "employees.pdf", FileUploadPurpose.Assistants);

        // Create a vector-store
        var vectorStoreClient = provider.Client.GetVectorStoreClient();
        var result = await vectorStoreClient.CreateVectorStoreAsync(
            waitUntilCompleted: false,
            new VectorStoreCreationOptions
            {
                FileIds = { fileInfo.Id }
            });

        // Create a thread associated with a vector-store for the agent conversation.
        string threadId =
            await agent.CreateThreadAsync(
                new OpenAIThreadCreationOptions
                {
                    VectorStoreId = result.VectorStoreId
                });

        // Respond to user input
        try
        {
            await InvokeAgentAsync("Who is the youngest employee?");
            await InvokeAgentAsync("Who works in sales?");
            await InvokeAgentAsync("I have a customer request, who can help me?");
        }
        finally
        {
            await agent.DeleteThreadAsync(threadId);
            await agent.DeleteAsync();
            await vectorStoreClient.DeleteVectorStoreAsync(result.VectorStoreId);
            await fileClient.DeleteFileAsync(fileInfo.Id);
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