using System.Dynamic;
using System.Text.Json.Serialization;

namespace chatminimalapi.DTOs;

public class Setting
{
    public string Id { get; set; }
    public string TenantId { get; set; }
    public Configuration Configuration { get; set; }
}

public class Configuration
{
    public string Title { get; set; }
    public string WelcomeMessage { get; set; }
    public string Max_token { get; set; }
    public string Model { get; set; }
    public string Key { get; set; }
    public string BaseUrl { get; set; }
    public string Url { get; set; }
    public string Cors { get; set; }
    public string instruction { get; set; }
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; }
}

public class SettingResponse : Setting
{

}