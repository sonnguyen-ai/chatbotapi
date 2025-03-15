using chatminimalapi.DTOs;
using Microsoft.Azure.Cosmos;

namespace chatminimalapi.Repositories;

public class CosmosSettingsRepository : ISettingsRepository
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseName;
    private readonly string _containerName;

    public CosmosSettingsRepository(CosmosClient cosmosClient, string databaseName = "chatbot", string containerName = "settings")
    {
        _cosmosClient = cosmosClient;
        _databaseName = databaseName;
        _containerName = containerName;
    }

    public async Task<List<Setting>> GetAllSettingsAsync()
    {
        var container = _cosmosClient.GetContainer(_databaseName, _containerName);
        var query = "SELECT * FROM c";
        var iterator = container.GetItemQueryIterator<Setting>(query);

        var response = await iterator.ReadNextAsync();
        var settingsResponse = new List<Setting>();

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            return settingsResponse;
        }

        foreach (var setting in response)
        {
            settingsResponse.Add(new Setting
            {
                Id = setting.Id,
                TenantId = setting.TenantId,
                Configuration = setting.Configuration
            });
        }

        return settingsResponse;
    }

    public async Task<FeedResponse<Message>> GetMessagesAsync(string tenantId, string databaseId)
    {
        var container = _cosmosClient.GetContainer(databaseId, "messages");
        var query = "SELECT * FROM c";

        var iterator = container.GetItemQueryIterator<Message>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(tenantId)
        });

        var response = await iterator.ReadNextAsync();
        return response;
    }

}