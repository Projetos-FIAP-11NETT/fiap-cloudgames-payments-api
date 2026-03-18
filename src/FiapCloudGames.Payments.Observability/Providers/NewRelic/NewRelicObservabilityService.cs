using FiapCloudGames.Payments.Observability.Abstractions;

namespace FiapCloudGames.Payments.Observability.Providers.NewRelic;

public class NewRelicObservabilityService : IObservabilityService
{
    public void AddCustomAttribute(string key, object value)
    {
        global::NewRelic.Api.Agent.NewRelic
            .GetAgent()
            .CurrentTransaction
            .AddCustomAttribute(key, value);
    }

    public void NoticeError(Exception exception)
    {
        global::NewRelic.Api.Agent.NewRelic
            .NoticeError(exception);
    }

    public void SetTransactionName(string category, string name)
    {
        global::NewRelic.Api.Agent.NewRelic
            .SetTransactionName(category, name);
    }
}
