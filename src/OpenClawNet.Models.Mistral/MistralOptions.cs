namespace OpenClawNet.Models.Mistral;

public sealed class MistralOptions
{
    public string? Endpoint { get; set; } = "https://api.mistral.ai/v1";
    public string? ApiKey { get; set; }
    public string? Model { get; set; } = "mistral-small-latest";
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
}
