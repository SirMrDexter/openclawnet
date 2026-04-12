using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AI.Foundry.Local;
using OpenClawNet.Models.Abstractions;

namespace OpenClawNet.Models.FoundryLocal;

/// <summary>
/// IModelClient implementation using Microsoft Foundry Local SDK.
/// Runs models on-device with zero cloud dependency.
/// </summary>
public sealed class FoundryLocalModelClient : IModelClient, IDisposable
{
    private readonly FoundryLocalOptions _options;
    private readonly ILogger<FoundryLocalModelClient> _logger;
    private FoundryLocalManager? _manager;
    private bool _initialized;

    public FoundryLocalModelClient(IOptions<FoundryLocalOptions> options, ILogger<FoundryLocalModelClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "foundry-local";

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized) return;

        var config = new Configuration(appName: _options.AppName);
        FoundryLocalManager.Initialize(config);
        _manager = FoundryLocalManager.Instance;

        var model = _manager.Catalog.GetModel(_options.Model);
        await model.DownloadAsync(ct);
        model.Load();

        _initialized = true;
        _logger.LogInformation("Foundry Local initialized with model {Model}", _options.Model);
    }

    public async Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var model = _manager!.Catalog.GetModel(request.Model ?? _options.Model);
        var client = model.GetChatClient();

        var messages = request.Messages.Select(m => new Dictionary<string, string>
        {
            ["role"] = m.Role switch
            {
                ChatMessageRole.System => "system",
                ChatMessageRole.User => "user",
                ChatMessageRole.Assistant => "assistant",
                ChatMessageRole.Tool => "tool",
                _ => "user"
            },
            ["content"] = m.Content
        }).ToList();

        var response = await client.CompleteChatAsync(messages, new()
        {
            Temperature = (float)(request.Temperature ?? _options.Temperature),
            MaxTokens = request.MaxTokens ?? _options.MaxTokens
        }, cancellationToken);

        return new ChatResponse
        {
            Content = response.Choices[0].Message.Content ?? string.Empty,
            Role = ChatMessageRole.Assistant,
            Model = request.Model ?? _options.Model,
            Usage = response.Usage is not null ? new UsageInfo
            {
                PromptTokens = response.Usage.PromptTokens,
                CompletionTokens = response.Usage.CompletionTokens,
                TotalTokens = response.Usage.TotalTokens
            } : null,
            FinishReason = "stop"
        };
    }

    public async IAsyncEnumerable<ChatResponseChunk> StreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var model = _manager!.Catalog.GetModel(request.Model ?? _options.Model);
        var client = model.GetChatClient();

        var messages = request.Messages.Select(m => new Dictionary<string, string>
        {
            ["role"] = m.Role switch
            {
                ChatMessageRole.System => "system",
                ChatMessageRole.User => "user",
                ChatMessageRole.Assistant => "assistant",
                ChatMessageRole.Tool => "tool",
                _ => "user"
            },
            ["content"] = m.Content
        }).ToList();

        await foreach (var chunk in client.CompleteChatStreamingAsync(messages, new()
        {
            Temperature = (float)(request.Temperature ?? _options.Temperature),
            MaxTokens = request.MaxTokens ?? _options.MaxTokens
        }, cancellationToken))
        {
            yield return new ChatResponseChunk
            {
                Content = chunk.ContentUpdate,
                FinishReason = chunk.FinishReason
            };
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureInitializedAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        // Foundry Local manager handles cleanup
        _manager = null;
        _initialized = false;
    }
}
