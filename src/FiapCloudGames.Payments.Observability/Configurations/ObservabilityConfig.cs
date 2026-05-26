using FiapCloudGames.Payments.Observability.Abstractions;
using FiapCloudGames.Payments.Observability.Providers.NewRelic;
using Microsoft.Extensions.DependencyInjection;

namespace FiapCloudGames.Payments.Observability.Configurations;

public static class ObservabilityConfig
{
    public static IServiceCollection AddObservabilityConfig(this IServiceCollection services)
    {
        services.AddScoped<IObservabilityService, NewRelicObservabilityService>();
        services.AddScoped(typeof(NewRelicConsumeFilter<>));
        services.AddScoped(typeof(NewRelicPublishFilter<>));

        return services;
    }
}
