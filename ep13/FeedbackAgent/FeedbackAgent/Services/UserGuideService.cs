using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureCosmosDBNoSQL;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Embeddings;


public partial class UserGuideService
{
    ITextEmbeddingGenerationService textEmbeddingGenerationService;
    IVectorStoreRecordCollection<AzureCosmosDBNoSQLCompositeKey, UserGuide> collection;

    CosmosDbService cosmosDbService;

    public UserGuideService()
    {
        cosmosDbService = new CosmosDbService(nameof(UserGuide));
        var vectorStore = new AzureCosmosDBNoSQLVectorStore(cosmosDbService.Database);

        textEmbeddingGenerationService = new AzureOpenAITextEmbeddingGenerationService(
            "text-embedding-3-small",
            Credential.EndPoint, Credential.ApiKey);

        var options = new AzureCosmosDBNoSQLVectorStoreRecordCollectionOptions<UserGuide>
        {
            PartitionKeyPropertyName = nameof(UserGuide.PartitionKey)
        };

        collection = new AzureCosmosDBNoSQLVectorStoreRecordCollection<UserGuide>(cosmosDbService.Database, "UserGuide", options);
    }

    [KernelFunction]
    public async Task<UserGuide> Upsert(UserGuide userGuide)
    {
        var embedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(userGuide.FullText);
        userGuide.Embedding = embedding;

        await collection.UpsertAsync(userGuide);
        return userGuide;
    }

    [KernelFunction]
    public async Task<UserGuide?> Get(string id)
    {
        var userGuide = await collection.GetAsync(new AzureCosmosDBNoSQLCompositeKey(nameof(UserGuide), id));
        return userGuide;
    }

    [KernelFunction]
    public async Task<List<VectorSearchResult<UserGuide>>> Search(string query)
    {
        var vectorSearchOptions = new VectorSearchOptions<UserGuide>
        {
            Top = 3
        };

        var searchVector = await textEmbeddingGenerationService.GenerateEmbeddingAsync(query);
        var searchResult = await collection.VectorizedSearchAsync(searchVector, vectorSearchOptions);

        var searchResultItems = await searchResult.Results.ToListAsync();
        return searchResultItems;
    }

   
}