using chatminimalapi.DTOs;
using Microsoft.Azure.Cosmos;

namespace chatminimalapi.Repositories;

public interface ISettingsRepository
{
    Task<List<Setting>> GetAllSettingsAsync();
    Task<FeedResponse<Message>> GetMessagesAsync(string tenantId,string databaseId);
}