using chatminimalapi.DTOs;
using chatminimalapi.Repositories;

public class SettingProvider
{
    private readonly ISettingsRepository _settingsRepository;
    public List<Setting> settings { get; private set; } = new List<Setting>();

    public SettingProvider(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task RefreshData()
    {
        settings = await _settingsRepository.GetAllSettingsAsync();
    }
}
