using Microsoft.Extensions.VectorData;

namespace WithInMemoryDemo;

public record Ship
{
    [VectorStoreRecordKey]
    public ulong ShipId { get; init; }

    [VectorStoreRecordData(IsFilterable = true)]
    public string? ShipName { get; init; }

    [VectorStoreRecordData(IsFullTextSearchable = true)]
    public string? ShipDescription { get; init; }

    [VectorStoreRecordVector(Dimensions: 1536)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; init; }

    [VectorStoreRecordData]
    public string[]? Tags { get; init; }
}