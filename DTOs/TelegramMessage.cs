using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace chatminimalapi.DTOs;

public class TelegramMessage
{
    [JsonPropertyName("message_id")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public int Id { get; set; }
    public string? Text { get; set; }
    public Chat Chat { get; set; }
}

public partial class Chat
{
    /// <summary>Unique identifier for this chat.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public long Id { get; set; }
}
