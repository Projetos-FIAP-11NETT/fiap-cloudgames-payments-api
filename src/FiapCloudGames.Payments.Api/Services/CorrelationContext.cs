using FiapCloudGames.Payments.Application.Interfaces;

namespace FiapCloudGames.Payments.Api.Services;

public class CorrelationContext : ICorrelationContext
{
    public string CorrelationId { get; private set; } = "n/a";

    public void SetCorrelationId(string correlationId)
    {
        if (!string.IsNullOrWhiteSpace(correlationId))
            CorrelationId = correlationId;
    }
}
