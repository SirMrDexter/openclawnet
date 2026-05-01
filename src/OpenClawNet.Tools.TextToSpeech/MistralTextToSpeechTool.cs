using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenClawNet.Storage;
using OpenClawNet.Tools.Abstractions;

namespace OpenClawNet.Tools.TextToSpeech;

public sealed class MistralTextToSpeechTool : ITool
{
    private readonly StorageOptions _storage;
    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MistralTextToSpeechTool> _logger;

    public MistralTextToSpeechTool(StorageOptions storage, HttpClient httpClient, IServiceScopeFactory scopeFactory, ILogger<MistralTextToSpeechTool> logger)
    {
        _storage = storage;
        _httpClient = httpClient;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public string Name => "mistral_text_to_speech";

    public string Description =>
        "Synthesize a MP3 from text using Mistral Voxtral TTS via API. " +
        "Returns the absolute path of the generated audio file.";

    public ToolMetadata Metadata => new()
    {
        Name = Name,
        Description = Description,
        ParameterSchema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "text": { "type": "string", "description": "Text to synthesize." }
            },
            "required": ["text"]
        }
        """),
        RequiresApproval = true,
        Category = "audio",
        Tags = ["audio", "tts", "speech", "mistral"]
    };

    public async Task<ToolResult> ExecuteAsync(ToolInput input, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var text = input.GetStringArgument("text");
            if (string.IsNullOrWhiteSpace(text))
                return ToolResult.Fail(Name, "'text' is required", sw.Elapsed);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var providerStore = scope.ServiceProvider.GetRequiredService<IModelProviderDefinitionStore>();

            var providers = await providerStore.ListAsync(cancellationToken);
            var mistralProvider = providers.FirstOrDefault(p => p.ProviderType == "mistral" && p.IsSupported);

            if (mistralProvider is null)
                return ToolResult.Fail(Name, "No supported Mistral model provider found. Please configure Mistral in Model Providers settings.", sw.Elapsed);

            if (string.IsNullOrWhiteSpace(mistralProvider.ApiKey))
                return ToolResult.Fail(Name, "Mistral provider is missing an API Key.", sw.Elapsed);

            if (string.IsNullOrWhiteSpace(mistralProvider.VoiceId))
                return ToolResult.Fail(Name, "Mistral provider is missing a Voice ID.", sw.Elapsed);

            var endpoint = string.IsNullOrWhiteSpace(mistralProvider.Endpoint) ? "https://api.mistral.ai/v1/" : mistralProvider.Endpoint.TrimEnd('/') + "/";
            var model = string.IsNullOrWhiteSpace(mistralProvider.Model) ? "mistral-small-latest" : mistralProvider.Model;
            // The default TTS model for mistral
            var ttsModel = model == "mistral-small-latest" ? "voxtral-mini-tts-2603" : "voxtral-mini-tts-2603";

            _logger.LogInformation("Synthesizing speech via Mistral API: voice={Voice} chars={Chars}", mistralProvider.VoiceId, text.Length);

            var requestPayload = new
            {
                model = ttsModel,
                input = text,
                voice_id = mistralProvider.VoiceId,
                response_format = "mp3"
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(endpoint), "audio/speech"))
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", mistralProvider.ApiKey) },
                Content = JsonContent.Create(requestPayload)
            };

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Mistral TTS API returned {StatusCode}: {Body}", response.StatusCode, body);
                return ToolResult.Fail(Name, $"Mistral API Error: {response.StatusCode} - {body}", sw.Elapsed);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var jsonDoc = JsonDocument.Parse(responseBody);
            
            if (!jsonDoc.RootElement.TryGetProperty("audio_data", out var audioDataProp) || audioDataProp.ValueKind != JsonValueKind.String)
            {
                return ToolResult.Fail(Name, "Mistral API response did not contain valid 'audio_data' property.", sw.Elapsed);
            }

            var base64Audio = audioDataProp.GetString()!;
            var audioData = Convert.FromBase64String(base64Audio);

            var outDir = _storage.BinaryFolderForTool("mistral-text-to-speech");
            var fileName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-mistral.mp3";
            var fullPath = Path.Combine(outDir, fileName);

            await File.WriteAllBytesAsync(fullPath, audioData, cancellationToken);
            sw.Stop();

            return ToolResult.Ok(Name, $"Saved to: {fullPath}\nVoice: {mistralProvider.VoiceId}", sw.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MistralTextToSpeech tool error");
            return ToolResult.Fail(Name, ex.Message, sw.Elapsed);
        }
    }
}
