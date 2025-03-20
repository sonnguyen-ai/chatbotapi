using System.Collections.Concurrent;

namespace chatminimalapi.DTOs;

public class ChatMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ChatHistory
{
    private static readonly ConcurrentDictionary<long, List<ChatMessage>> _chatHistories = new();
    private static readonly object _lockObject = new();
    private const int MAX_HISTORY_MESSAGES = 10;
    public long ChatId { get; set; }
    public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    public static void AddMessage(long chatId, ChatMessage message)
    {
        _chatHistories.AddOrUpdate(chatId, new List<ChatMessage> { message },
        (_, existingMessages) =>
        {
            lock (_lockObject)
            {
                existingMessages.Add(message);
                return existingMessages.Skip(Math.Max(0, existingMessages.Count - MAX_HISTORY_MESSAGES)).ToList();
            }
        });
    }

    public static List<ChatMessage> GetHistory(long chatId)
    {
        return _chatHistories.GetValueOrDefault(chatId, new List<ChatMessage>());
    }

    public static void ClearHistory(long chatId)
    {
        _ = _chatHistories.TryRemove(chatId, out _);
    }
}
