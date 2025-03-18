namespace chatminimalapi.DTOs;

public class TelegramMessage
{
    public long ChatId { get; set; } // Changed to long to match Telegram's chat_id
    public string? Text { get; set; }
}


