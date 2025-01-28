using System.ClientModel;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;

using OpenAI;

namespace TextSearchWithVectorStore.ConsoleApp;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class TextSearchService(IConfiguration config)
{
    private static readonly string[] entries =
    [
        "Semantic Kernel is a lightweight, open-source development kit that lets you easily build AI agents and integrate the latest AI models into your C#, Python, or Java codebase. It serves as an efficient middleware that enables rapid delivery of enterprise-grade solutions.",
        "Semantic Kernel is a new AI SDK, and a simple and yet powerful programming model that lets you add large language capabilities to your app in just a matter of minutes. It uses natural language prompting to create and execute semantic kernel AI tasks across multiple languages and platforms.",
        "In this guide, you learned how to quickly get started with Semantic Kernel by building a simple AI agent that can interact with an AI service and run your code. To see more examples and learn how to build more complex AI agents, check out our in-depth samples.",
        "The Semantic Kernel extension for Visual Studio Code makes it easy to design and test semantic functions.The extension provides an interface for designing semantic functions and allows you to test them with the push of a button with your existing models and data.",
        "The kernel is the central component of Semantic Kernel.At its simplest, the kernel is a Dependency Injection container that manages all of the services and plugins necessary to run your AI application.",
        "Semantic Kernel (SK) is a lightweight SDK that lets you mix conventional programming languages, like C# and Python, with the latest in Large Language Model (LLM) AI “prompts” with prompt templating, chaining, and planning capabilities.",
        "Semantic Kernel is a lightweight, open-source development kit that lets you easily build AI agents and integrate the latest AI models into your C#, Python, or Java codebase. It serves as an efficient middleware that enables rapid delivery of enterprise-grade solutions. Enterprise ready.",
        "With Semantic Kernel, you can easily build agents that can call your existing code.This power lets you automate your business processes with models from OpenAI, Azure OpenAI, Hugging Face, and more! We often get asked though, “How do I architect my solution?” and “How does it actually work?”"
    ];

    public async Task<IVectorStoreRecordCollection<Guid, DataModel>> GetVectorStoreRecordCollectionAsync(string collectionName)
    {
        var store = new InMemoryVectorStore();
        var collection = store.GetCollection<Guid, DataModel>(collectionName);
        await collection.CreateCollectionIfNotExistsAsync().ConfigureAwait(false);

        return collection;
    }

    public async Task<VectorStoreTextSearch<DataModel>> GetVectorStoreTextSearchAsync(IVectorStoreRecordCollection<Guid, DataModel> collection)
    {
        var embeddingsClient = new OpenAIClient(new ApiKeyCredential(config["GitHub:Models:1:Token"]!), new OpenAIClientOptions { Endpoint = new Uri(config["GitHub:Models:1:Endpoint"]!) });
        var embeddingsService = new OpenAITextEmbeddingGenerationService(config["GitHub:Models:1:ModelId"]!, embeddingsClient);

        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            var embedding = await embeddingsService.GenerateEmbeddingAsync(entry).ConfigureAwait(false);

            var guid = Guid.NewGuid();
            var record = new DataModel()
            {
                Key = guid,
                Text = entry,
                Link = $"guid://{guid}",
                Tag = i % 2 == 0 ? "Even" : "Odd",
                Embedding = embedding
            };

            await collection.UpsertAsync(record).ConfigureAwait(false);
        }

        var search = new VectorStoreTextSearch<DataModel>(collection, embeddingsService);

        return search;
    }
}

#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
