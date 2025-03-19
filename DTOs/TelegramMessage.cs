using Newtonsoft.Json;

namespace chatminimalapi.DTOs;

public class TelegramMessage
{

    [JsonProperty("message_id")]
    public long MesageId { get; set; }
    public string? Text { get; set; }
}


