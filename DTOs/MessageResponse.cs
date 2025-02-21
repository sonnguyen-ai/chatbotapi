namespace chatminimalapi.DTOs;

public class MessageResponse
{
    public Guid Id { get; set; }
    public string Text { get; set; }
    public bool HideInChat { get; set; }
    public string Role { get; set; }
}