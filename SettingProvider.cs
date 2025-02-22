using chatminimalapi.DTOs;
using Microsoft.Azure.Cosmos;

public class SettingProvider{
    public List<Setting> settings { get; set; }

    public async Task RefreshData(CosmosClient cosmosClient)
    {
        settings = await GetSettings(cosmosClient);
    }

    private async Task<List<Setting>> GetSettings(CosmosClient cosmosClient)
    {
        var container = cosmosClient.GetContainer("chatbot", "settings");
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
}