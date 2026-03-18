using FiapCloudGames.Payments.Observability.Abstractions;
using MassTransit;

namespace FiapCloudGames.Payments.Observability.Providers.NewRelic;

public class NewRelicConsumeFilter<T>(IObservabilityService obs) : IFilter<ConsumeContext<T>> where T : class
{
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var queue = context.ReceiveContext.InputAddress?.AbsolutePath ?? "unknown";
        var message = typeof(T).Name;

        obs.SetTransactionName("MassTransit", $"/{queue}/{message}");
        obs.AddCustomAttribute("MessageType", typeof(T).Name);
        obs.AddCustomAttribute("CorrelationId", context.CorrelationId?.ToString() ?? string.Empty);

        try
        {
            await next.Send(context);
        }
        catch (Exception ex)
        {
            obs.NoticeError(ex);
            throw;
        }
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("newRelicConsumeFilter");
    }
}
