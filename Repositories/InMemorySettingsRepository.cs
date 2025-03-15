using chatminimalapi.DTOs;
using Microsoft.Azure.Cosmos;

namespace chatminimalapi.Repositories;

public class InMemorySettingsRepository : ISettingsRepository
{
    private readonly List<Setting> _settings = new();

    public InMemorySettingsRepository(IEnumerable<Setting> initialSettings = null)
    {
        if (initialSettings != null)
        {
            _settings.AddRange(initialSettings);
        }
    }

    public Task<List<Setting>> GetAllSettingsAsync()
    {
        return Task.FromResult(_settings.ToList());
    }

    public Task<FeedResponse<Message>> GetMessagesAsync(string tenantId, string databaseId)
    {
        throw new NotImplementedException();
    }
}