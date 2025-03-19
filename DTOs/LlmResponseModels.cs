using System.Collections.Generic;
using Newtonsoft.Json;
namespace chatminimalapi.DTOs;

public class LlmApiResponse
{
    [JsonProperty("candidates")]
    public List<Candidate> Candidates { get; set; }

    [JsonProperty("usageMetadata")]
    public UsageMetadata UsageMetadata { get; set; }

    [JsonProperty("modelVersion")]
    public string ModelVersion { get; set; }
}

public class Candidate
{
    [JsonProperty("content")]
    public Content Content { get; set; }

    [JsonProperty("finishReason")]
    public string FinishReason { get; set; }

    [JsonProperty("safetyRatings")]
    public List<SafetyRating> SafetyRatings { get; set; }

    [JsonProperty("avgLogprobs")]
    public double AvgLogprobs { get; set; }
}

public class Content
{
    [JsonProperty("parts")]
    public List<Part> Parts { get; set; }

    [JsonProperty("role")]
    public string Role { get; set; }
}

public class SafetyRating
{
    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("probability")]
    public string Probability { get; set; }
}

public class UsageMetadata
{
    [JsonProperty("promptTokenCount")]
    public int PromptTokenCount { get; set; }

    [JsonProperty("candidatesTokenCount")]
    public int CandidatesTokenCount { get; set; }

    [JsonProperty("totalTokenCount")]
    public int TotalTokenCount { get; set; }

    [JsonProperty("promptTokensDetails")]
    public List<TokenDetails> PromptTokensDetails { get; set; }

    [JsonProperty("candidatesTokensDetails")]
    public List<TokenDetails> CandidatesTokensDetails { get; set; }
}

public class TokenDetails
{
    [JsonProperty("modality")]
    public string Modality { get; set; }

    [JsonProperty("tokenCount")]
    public int TokenCount { get; set; }
}
