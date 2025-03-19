namespace chatminimalapi.DTOs;

public class ChatMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ChatHistory
{
    private static readonly Dictionary<long, List<ChatMessage>> _chatHistories = new();
    private const int MAX_HISTORY_MESSAGES = 10;
    public long ChatId { get; set; }
    public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    public static void AddMessage(long chatId, ChatMessage message)
    {
        if (!_chatHistories.ContainsKey(chatId))
        {
            _chatHistories[chatId] = new List<ChatMessage>();
        }

        _chatHistories[chatId].Add(message);

        // Keep only the last MAX_HISTORY_MESSAGES messages
        if (_chatHistories[chatId].Count > MAX_HISTORY_MESSAGES)
        {
            _chatHistories[chatId] = _chatHistories[chatId]
                .Skip(_chatHistories[chatId].Count - MAX_HISTORY_MESSAGES)
                .ToList();
        }
    }

    public static List<ChatMessage> GetHistory(long chatId)
    {
        if (_chatHistories.ContainsKey(chatId))
        {
            return _chatHistories[chatId].ToList();
        }
        return new List<ChatMessage>();
    }

    public static void ClearHistory(long chatId)
    {
        if (_chatHistories.ContainsKey(chatId))
        {
            _chatHistories.Remove(chatId);
        }
    }
}
