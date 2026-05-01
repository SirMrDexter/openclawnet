using Microsoft.Extensions.DependencyInjection;
using OpenClawNet.Models.Abstractions;

namespace OpenClawNet.Models.Mistral;

public static class MistralServiceCollectionExtensions
{
    public static IServiceCollection AddMistral(this IServiceCollection services, Action<MistralOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<MistralOptions>(_ => { });

        services.AddHttpClient<MistralModelClient>();
        services.AddSingleton<IModelClient>(sp => sp.GetRequiredService<MistralModelClient>());

        return services;
    }
}
