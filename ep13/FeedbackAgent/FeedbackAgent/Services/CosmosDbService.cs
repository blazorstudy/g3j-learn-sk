using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.ComponentModel;
using System.Net;

public class CosmosDbService
{
    public CosmosClient CosmosClient { get; }

    public Database Database { get; }

    public Microsoft.Azure.Cosmos.Container Container { get; set; }

    public CosmosDbService(string containerName)
    {
        var defaultSerializer = new DefaultJsonCosmosSerializer();

        CosmosClient = new CosmosClient(Credential.CosmosDbConnectionString, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            MaxRetryAttemptsOnRateLimitedRequests = 60,
            MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromMinutes(30),
            RequestTimeout = TimeSpan.FromMinutes(2),
            UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Default,
        });

        var database = CosmosClient.GetDatabase("DB");
        Database = database;

        Container = database.GetContainer(containerName);
    }

    public async Task<T?> Get<T>(string partitionKey, string id) where T : ICosmosModel
    {
        try
        {
            var pk = new PartitionKey(partitionKey);
            var response = await Container.ReadItemAsync<T>(id, pk).ConfigureAwait(false);
            var result = response.Resource;

            return (T)result;
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
                return default;
            else
                throw;
        }
    }

    public async Task<T> Upsert<T>(T item) where T : ICosmosModel
    {
        if (string.IsNullOrEmpty(item.PartitionKey))
            throw new ArgumentException("Partition Key 값이 없습니다.");

        var response = await Container.UpsertItemAsync(item).ConfigureAwait(false);
        return response.Resource;
    }


}

public interface ICosmosModel
{
    [JsonPropertyName("id")]
    string Id { get; set; }

    public string PartitionKey { get; set; }
}

public class DefaultJsonCosmosSerializer : CosmosSerializer
{
    private readonly JsonObjectSerializer _systemTextJsonSerializer;

    public DefaultJsonCosmosSerializer()
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };

        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        _systemTextJsonSerializer = new JsonObjectSerializer(jsonOptions);
    }

    public override T FromStream<T>(Stream stream)
    {
        try
        {
            if (stream.CanSeek && stream.Length == 0)
            {
                return default;
            }

            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }

            using (var sr = new StreamReader(stream))
            {
                return (T)_systemTextJsonSerializer.Deserialize(stream, typeof(T), default);
            }
        }
        catch (Exception ex)
        {
        }

        return default(T);
    }

    public override Stream ToStream<T>(T input)
    {
        var streamPayload = new MemoryStream();
        _systemTextJsonSerializer.Serialize(streamPayload, input, typeof(T), default);
        streamPayload.Position = 0;
        return streamPayload;
    }
}

