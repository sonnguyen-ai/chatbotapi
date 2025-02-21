namespace chatminimalapi.DTOs;

using System.Collections.Generic;

public class Part
{
    public string Text { get; set; }
}

public class MessageRequest
{
    public string Role { get; set; }
    public List<Part> parts { get; set; }
}


