using Amazon.SimpleNotificationService;
using Amazon.SQS;
using FiapCloudGames.Payments.Observability.Providers.NewRelic;
using FiapCloudGames.Queue.Configurations.MassTransit;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Queue.Configurations.Sqs;

public static class SqsStartup
{
    public static void RegisterSqsStartup(this IServiceCollection services)
    {
        services.AddMassTransit<ISqsPublish>(x =>
        {
            x.AddConsumers(GetConsumers());

            x.SetEndpointNameFormatter(
                new KebabCaseEndpointNameFormatter("payments", false));

            x.UsingAmazonSqs((context, cfg) =>
            {
                var sqsSettings = context.GetRequiredService<IOptions<SqsSettings>>().Value;
                var massTransitSettings = context.GetRequiredService<IOptions<MassTransitSettings>>().Value;

                cfg.Host(sqsSettings.Region, h =>
                {
                    h.AccessKey(sqsSettings.AccessKey);
                    h.SecretKey(sqsSettings.SecretKey);

                    if (!string.IsNullOrWhiteSpace(sqsSettings.ServiceUrl))
                    {
                        h.Config(new AmazonSQSConfig
                        {
                            ServiceURL = sqsSettings.ServiceUrl,
                            AuthenticationRegion = sqsSettings.Region
                        });

                        h.Config(new AmazonSimpleNotificationServiceConfig
                        {
                            ServiceURL = sqsSettings.ServiceUrl,
                            AuthenticationRegion = sqsSettings.Region
                        });
                    }
                });

                cfg.UseMessageRetry(r => r.Interval(massTransitSettings.RetryCount, massTransitSettings.Interval));

                cfg.UseConsumeFilter(typeof(NewRelicConsumeFilter<>), context);
                cfg.UsePublishFilter(typeof(NewRelicPublishFilter<>), context);

                cfg.ConfigureEndpoints(context);
            });
        });
    }

    private static Type[] GetConsumers()
        => AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(p => typeof(IConsumer).IsAssignableFrom(p) &&
                        p.Namespace != null &&
                        p.Namespace.Contains("FiapCloudGames.Queue.Consumers.Sqs"))
            .ToArray();
}