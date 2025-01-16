using Azure;
using Azure.Core;
using Azure.Search.Documents.Indexes;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.AzureCosmosDBNoSQL;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using OpenAI.VectorStores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor.ContentOrderTextExtractor;

namespace VectorStoreExercise
{
    public class VectorStoreService
    {
        ITextEmbeddingGenerationService textEmbeddingGenerationService;
        IVectorStoreRecordCollection<string, TodoItem> collection;
        public VectorStoreTextSearch<TodoItem> TextSearch { get; set; }
        public VectorStoreService()
        {
            textEmbeddingGenerationService = new AzureOpenAITextEmbeddingGenerationService(
                "text-embedding-3-small",
                Credential.EndPoint, Credential.ApiKey);

            //var vectorStore = new InMemoryVectorStore();

            //var vectorStore = new AzureAISearchVectorStore(new SearchIndexClient(
            //    new Uri(Credential.AzureAiSearchEndpoint),
            //    new AzureKeyCredential(Credential.AzureAiSearchKey)));
            //
            //collection = vectorStore.GetCollection<string, TodoItem>("sktodo");

            var cosmosClient = new CosmosClient(Credential.AzureCosmosDBConnectionString, new CosmosClientOptions()
            {
                UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Default
            });

            var database = cosmosClient.GetDatabase("DB");

            var vectorStore = new AzureCosmosDBNoSQLVectorStore(database);

            collection = new AzureCosmosDBNoSQLVectorStoreRecordCollection<TodoItem>(
                database,
                "sktodo",
                new()
                {
                    PartitionKeyPropertyName = nameof(TodoItem.PartitionKey)
                });

            TextSearch = new VectorStoreTextSearch<TodoItem>(collection, textEmbeddingGenerationService);
        }

        public async Task<IEnumerable<string>> Load()
        {
            await collection.CreateCollectionIfNotExistsAsync();

            var todoEntries = CreateTodoList().ToList();
            var tasks = todoEntries.Select(entry => Task.Run(async () =>
            {
                entry.Embedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(entry.Description);
            }));
            await Task.WhenAll(tasks);

            var upsertedKeysTasks = todoEntries.Select(x => collection.UpsertAsync(x));
            return await Task.WhenAll(upsertedKeysTasks);
        }

        public async Task<List<VectorSearchResult<TodoItem>>> Search(string searchString)
        {
            var searchVector = await textEmbeddingGenerationService.GenerateEmbeddingAsync(searchString);
            var searchResult = await collection.VectorizedSearchAsync(
                searchVector,
                new()
                {
                    Top = 3
                });
            var searchResultItems = await searchResult.Results.ToListAsync();
            return searchResultItems;
        }

        private IEnumerable<TodoItem> CreateTodoList()
        {
            yield return new TodoItem
            {
                Key = "1",
                Category = "소프트웨어",
                Title = "API 공부하기",
                Description = "API(Application Programming Interface)에 대해 배우고, 소프트웨어 간의 데이터 교환 방식을 이해해 보기",
                Link = "link#1"
            };

            yield return new TodoItem
            {
                Key = "2",
                Category = "소프트웨어",
                Title = "SDK 설치 및 사용 연습",
                Description = "SDK(Software Development Kit)를 설치하고 간단한 프로젝트를 통해 사용법을 익혀보기",
                Link = "link#2"
            };

            yield return new TodoItem
            {
                Key = "3",
                Category = "AI",
                Title = "Semantic Kernel Connectors 탐구",
                Description = "AI 서비스와 통합을 위한 Semantic Kernel Connectors를 살펴보고 활용 가능성을 알아보기",
                Link = "link#3"
            };

            yield return new TodoItem
            {
                Key = "4",
                Category = "AI",
                Title = "Semantic Kernel 이해하기",
                Description = "AI 애플리케이션 개발에 도움을 주는 Semantic Kernel 라이브러리를 학습하고 활용해 보기",
                Link = "link#4"
            };

            yield return new TodoItem
            {
                Key = "5",
                Category = "AI",
                Title = "RAG 개념 익히기",
                Description = "RAG(Retrieval Augmented Generation)의 개념을 배우고, 추가 데이터를 사용하여 응답 품질을 향상시키는 방법을 알아보기",
                Link = "link#5"
            };

            yield return new TodoItem
            {
                Key = "6",
                Category = "AI",
                Title = "LLM 이해 및 실습",
                Description = "LLM(Large Language Model)을 이해하고, 이를 사용해 인간 언어를 생성하는 실습을 해보기",
                Link = "link#6"
            };
            yield return new TodoItem
            {
                Key = "7",
                Category = "쇼핑",
                Title = "우유 사기",
                Description = "신선한 우유 구매",
                Link = "link#7"
            };

            yield return new TodoItem
            {
                Key = "8",
                Category = "쇼핑",
                Title = "계란 사기",
                Description = "유통기한이 넉넉한 계란 구매",
                Link = "link#8"
            };

            yield return new TodoItem
            {
                Key = "9",
                Category = "쇼핑",
                Title = "빵 사기",
                Description = "간단히 먹을 수 있는 빵 구매",
                Link = "link#9"
            };
        }
    }

    public class TodoItem
    {
        [JsonPropertyName("id")]
        public string Id { get => Key; set { Key = value; } }

        [VectorStoreRecordKey]
        [TextSearchResultName]
        public string Key { get; set; }

        [VectorStoreRecordData(IsFilterable = true)]
        public string Category { get; set; }

        [VectorStoreRecordData]
        public string Title { get; set; }

        [VectorStoreRecordData]
        public string Description { get; set; }

        [VectorStoreRecordData]
        [TextSearchResultValue]
        public string EmbeddingData => @$"Title : {Title}
Description : {Description}
Category : {Category}";

        [VectorStoreRecordData]
        [TextSearchResultLink]
        public string Link { get; set; }

        [VectorStoreRecordData]
        public string PartitionKey { get; set; } = "1";

        [VectorStoreRecordVector(Dimensions:1536)]
        public ReadOnlyMemory<float> Embedding { get; set; }
    }
}
