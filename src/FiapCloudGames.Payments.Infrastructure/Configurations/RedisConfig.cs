using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiapCloudGames.Payments.Infrastructure.Configurations;

public static class RedisConfig
{
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection("Redis")["ConnectionString"];

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
        });

        return services;
    }
}
