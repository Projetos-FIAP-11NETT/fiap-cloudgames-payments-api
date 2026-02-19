using FiapCloudGames.Payments.Application.Behaviors;
using FiapCloudGames.Payments.Application.Interfaces;
using FiapCloudGames.Payments.Infrastructure.Data;
using FiapCloudGames.Payments.Infrastructure.Repositories;
using FiapCloudGames.Queue.Configurations;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace FiapCloudGames.Payments.Api.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDatabase(configuration);

        // Queue / RabbitMQ
        services.AddQueueConfig(configuration);

        // MediatR
        services.AddMediatRServices();

        // Validators
        services.AddValidators();

        // Repositories
        services.AddRepositories();

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentsDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("FiapCloudGames.Payments.Infrastructure")
            ));

        return services;
    }

    private static IServiceCollection AddMediatRServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                Assembly.Load("FiapCloudGames.Payments.Application"));

            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }

    private static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(
            Assembly.Load("FiapCloudGames.Payments.Application"));

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPaymentsRepository, PaymentsRepository>();

        return services;
    }
}
