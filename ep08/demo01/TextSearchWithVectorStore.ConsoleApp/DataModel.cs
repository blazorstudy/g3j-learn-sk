using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace TextSearchWithVectorStore.ConsoleApp;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class DataModel
{
    [VectorStoreRecordKey]
    [TextSearchResultName]
    public Guid Key { get; init; }

    [VectorStoreRecordData]
    [TextSearchResultValue]
    public string? Text { get; init; }

    [VectorStoreRecordData]
    [TextSearchResultLink]
    public string? Link { get; init; }

    [VectorStoreRecordData(IsFilterable = true)]
    public required string Tag { get; init; }

    [VectorStoreRecordVector]
    public ReadOnlyMemory<float> Embedding { get; init; }
}

#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
