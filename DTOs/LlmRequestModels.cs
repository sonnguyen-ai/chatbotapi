using System.Collections.Generic;
using Newtonsoft.Json;
namespace chatminimalapi.DTOs;

public class LlmRequest
{
    [JsonProperty("contents")]
    public List<Content> Contents { get; set; }

    [JsonProperty("safetySettings")]
    public List<SafetySetting> SafetySettings { get; set; }

    [JsonProperty("generationConfig")]
    public GenerationConfig GenerationConfig { get; set; }
}

public class SafetySetting
{
    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("threshold")]
    public string Threshold { get; set; }
}

public class GenerationConfig
{
    [JsonProperty("maxOutputTokens")]
    public int MaxOutputTokens { get; set; }

    [JsonProperty("temperature")]
    public double Temperature { get; set; }

    [JsonProperty("topK")]
    public int TopK { get; set; }

    [JsonProperty("topP")]
    public double TopP { get; set; }
}
