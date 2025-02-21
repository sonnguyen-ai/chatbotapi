namespace chatminimalapi.DTOs;

public class Message
{
    public Guid Id { get; set; }
    public string RoomId { get; set; }
    public Payload Payload { get; set; }
}

public class Payload {
    public string Text { get; set; }
    public bool HideInChat { get; set; }
    public string Role { get; set; }
}

    
