using System.Text.Json.Serialization;

namespace chatminimalapi.DTOs;

public class TelegramUpdate
{
    [JsonPropertyName("update_id")]
    public int UpdateId { get; set; }
    public TelegramMessage? Message { get; set; }
}


