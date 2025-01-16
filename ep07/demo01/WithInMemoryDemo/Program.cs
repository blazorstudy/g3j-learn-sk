#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010

using System.Text;

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Embeddings;

using WithInMemoryDemo;

Console.OutputEncoding = Encoding.UTF8;


var kernel = Kernel.CreateBuilder()
                   // OpenAIChatCompletion -> OpenAITextEmbeddingGeneration
                   .AddAzureOpenAITextEmbeddingGeneration(
                       Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDING_NAME")!,
                       Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,
                       Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!)
                   .AddInMemoryVectorStoreRecordCollection<ulong, Ship>("skships")
                   .Build();


var service = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

// Create a InMemory VectorStore object and choose an existing collection that already contains records.
var inMemoryVectorStore = new InMemoryVectorStore();
var vectorStoreRecordCollection = inMemoryVectorStore.GetCollection<ulong, Ship>("skships");

await vectorStoreRecordCollection.CreateCollectionIfNotExistsAsync();

await GenerateEmbeddingsAndUpsertAsync(service, vectorStoreRecordCollection);

// Generate a vector for your search text, using your chosen embedding generation implementation.
var searchVector = await service.GenerateEmbeddingAsync("15세기부터 17세기까지의 최고의 군용 범선 중에 가장 유명했던 배를 찾아줘.");

// Do the search, passing an options object with a Top value to limit resulst to the single top match.
var vectorSearchResults = await vectorStoreRecordCollection.VectorizedSearchAsync(searchVector, new() { Top = 1 });

// Inspect the returned hotel.
await foreach (var record in vectorSearchResults.Results)
{
    Console.WriteLine("Found ship description: " + record.Record.ShipDescription);
    Console.WriteLine("Found record score: " + record.Score);
}


Console.WriteLine("Bye!");




async Task GenerateEmbeddingsAndUpsertAsync(
    ITextEmbeddingGenerationService textEmbeddingGenerationService,
    IVectorStoreRecordCollection<ulong, Ship> collection)
{
    // Upsert a record.
    var descriptionText = "15세기부터 17세기까지의 최고의 군용 범선.";
    var shipId = 1ul;

    // Generate the embedding.
    ReadOnlyMemory<float> embedding =
        await textEmbeddingGenerationService.GenerateEmbeddingAsync(descriptionText);

    // Create a record and upsert with the already generated embedding.
    await collection.UpsertAsync(new Ship
    {
        ShipId = shipId,
        ShipName = "Frigate",
        ShipDescription = descriptionText,
        DescriptionEmbedding = embedding,
        Tags = ["warship", "sailboat"]
    });
}

async Task GenerateEmbeddingsAndSearchAsync(
    ITextEmbeddingGenerationService textEmbeddingGenerationService,
    IVectorStoreRecordCollection<ulong, Ship> collection)
{
    // Upsert a record.
    var descriptionText = "가장 유명했던 배를 찾아줘.";

    // Generate the embedding.
    ReadOnlyMemory<float> searchEmbedding =
        await textEmbeddingGenerationService.GenerateEmbeddingAsync(descriptionText);

    // Search using the already generated embedding.
    VectorSearchResults<Ship> searchResults = await collection.VectorizedSearchAsync(searchEmbedding);

    await foreach(var searchResult in searchResults.Results)
    {
        // Print the first search result.
        Console.WriteLine("Score for first result: " + searchResult.Score);
        Console.WriteLine("Ship description for first result: " + searchResult.Record.ShipDescription);
        break;
    }
}