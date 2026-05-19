using FiapCloudGames.Payments.Observability.Abstractions;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;

namespace FiapCloudGames.Payments.Observability.Providers.NewRelic;

public class NewRelicPublishFilter<T>(IObservabilityService obs) : IFilter<PublishContext<T>> where T : class
{
    public async Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        MassTransitHeadersAdapter.SetTraceHeaders(context.Headers);
        obs.AddCustomAttribute("PublishedMessageType", typeof(T).Name);
        obs.AddCustomAttribute("PublishedCorrelationId", context.CorrelationId?.ToString() ?? string.Empty);

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
        context.CreateFilterScope("newRelicPublishFilter");
    }
}
