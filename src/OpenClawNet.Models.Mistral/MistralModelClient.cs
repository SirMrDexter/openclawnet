using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenClawNet.Models.Abstractions;

namespace OpenClawNet.Models.Mistral;

public sealed class MistralModelClient : IModelClient
{
    private readonly HttpClient _httpClient;
    private readonly MistralOptions _options;
    private readonly ILogger<MistralModelClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MistralModelClient(HttpClient httpClient, IOptions<MistralOptions> options, ILogger<MistralModelClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        var endpoint = _options.Endpoint;
        if (string.IsNullOrEmpty(endpoint))
        {
            endpoint = "https://api.mistral.ai/v1/";
        }
        _httpClient.BaseAddress = new Uri(endpoint);

        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
    }

    public string ProviderName => "mistral";

    public async Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var payload = BuildPayload(request, stream: false);
        _logger.LogDebug("Sending chat to Mistral: model={Model}", request.Model ?? _options.Model);

        var response = await _httpClient.PostAsJsonAsync("chat/completions", payload, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MistralChatResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Empty response from Mistral");

        var choice = result.Choices?.FirstOrDefault();

        return new ChatResponse
        {
            Content = choice?.Message?.Content ?? string.Empty,
            Role = ChatMessageRole.Assistant,
            Model = result.Model ?? _options.Model,
            Usage = result.Usage is not null ? new UsageInfo
            {
                PromptTokens = result.Usage.PromptTokens,
                CompletionTokens = result.Usage.CompletionTokens,
                TotalTokens = result.Usage.TotalTokens
            } : null,
            FinishReason = choice?.FinishReason
        };
    }

    public async IAsyncEnumerable<ChatResponseChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var payload = BuildPayload(request, stream: true);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]") break;

            MistralStreamChunk? chunk;
            try { chunk = JsonSerializer.Deserialize<MistralStreamChunk>(data, JsonOptions); }
            catch { continue; }

            if (chunk is null) continue;

            var delta = chunk.Choices?.FirstOrDefault()?.Delta;
            yield return new ChatResponseChunk
            {
                Content = delta?.Content,
                FinishReason = chunk.Choices?.FirstOrDefault()?.FinishReason
            };
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
            return false;

        try
        {
            var response = await _httpClient.GetAsync("models", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
            throw new InvalidOperationException("Mistral is not configured. Set ApiKey.");
    }

    private object BuildPayload(ChatRequest request, bool stream)
    {
        return new
        {
            model = request.Model ?? _options.Model,
            messages = request.Messages.Select(m => new
            {
                role = m.Role.ToString().ToLowerInvariant(),
                content = m.Content
            }),
            temperature = request.Temperature ?? _options.Temperature,
            max_tokens = request.MaxTokens ?? _options.MaxTokens,
            stream
        };
    }
}

internal sealed class MistralChatResponse
{
    public string? Model { get; set; }
    public List<MistralChoice>? Choices { get; set; }
    public MistralUsage? Usage { get; set; }
}

internal sealed class MistralChoice
{
    public MistralMessage? Message { get; set; }
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

internal sealed class MistralMessage
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}

internal sealed class MistralUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

internal sealed class MistralStreamChunk
{
    public List<MistralStreamChoice>? Choices { get; set; }
}

internal sealed class MistralStreamChoice
{
    public MistralStreamDelta? Delta { get; set; }
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

internal sealed class MistralStreamDelta
{
    public string? Content { get; set; }
}
