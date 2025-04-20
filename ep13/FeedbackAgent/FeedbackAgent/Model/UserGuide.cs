using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.VectorData;
using OpenAI.VectorStores;
using System.Text.Json.Serialization;

public class UserGuide : ICosmosModel
{
    [JsonPropertyName("id")]
    [VectorStoreRecordKey]
    public string Id { get; set; }

    [VectorStoreRecordData]
    public string? Title { get; set; }

    [VectorStoreRecordData]
    public string? Guide { get; set; }

    public string FullText => 
@$"##{Title}

{Guide}";

    [VectorStoreRecordVector]
    public ReadOnlyMemory<float> Embedding { get; set; }

    [VectorStoreRecordData]
    public string PartitionKey { get; set; } = nameof(UserGuide);
}
