using System.Net.Http.Headers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenClawNet.Models.Abstractions;

namespace OpenClawNet.Models.Mistral;

public sealed class MistralAgentProvider : IAgentProvider
{
    private readonly IOptions<MistralOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<MistralAgentProvider> _logger;

    public MistralAgentProvider(
        IOptions<MistralOptions> options,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        ILogger<MistralAgentProvider> logger)
    {
        _options = options;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public string ProviderName => "mistral";

    public IChatClient CreateChatClient(AgentProfile profile)
    {
        var opts = _options.Value;
        var endpoint = profile.Endpoint ?? opts.Endpoint;
        if (string.IsNullOrEmpty(endpoint))
        {
            endpoint = "https://api.mistral.ai/v1";
        }
        var apiKey = profile.ApiKey ?? opts.ApiKey;

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Mistral is not configured. Set ApiKey.");

        _logger.LogDebug("Creating Mistral IChatClient: endpoint={Endpoint}, model={Model}", endpoint, opts.Model);

        var http = _httpClientFactory.CreateClient();
        var perCallOpts = Options.Create(new MistralOptions
        {
            Endpoint = endpoint,
            ApiKey = apiKey,
            Model = opts.Model,
            Temperature = profile.Temperature ?? opts.Temperature,
            MaxTokens = profile.MaxTokens ?? opts.MaxTokens,
        });

        var client = new MistralModelClient(http, perCallOpts, _loggerFactory.CreateLogger<MistralModelClient>());
        var innerClient = new MistralModelChatClientBridge(client);
        return new ChatClientBuilder(innerClient)
            .UseOpenTelemetry(sourceName: "OpenClawNet.Mistral")
            .Build();
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        var opts = _options.Value;
        if (string.IsNullOrEmpty(opts.ApiKey))
            return false;

        var endpoint = opts.Endpoint;
        if (string.IsNullOrEmpty(endpoint))
        {
            endpoint = "https://api.mistral.ai/v1";
        }

        try
        {
            using var http = _httpClientFactory.CreateClient();
            http.BaseAddress = new Uri(endpoint.TrimEnd('/'));
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opts.ApiKey);
            var response = await http.GetAsync("/models", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
