namespace FiapCloudGames.Payments.Application.Interfaces;

public interface ICorrelationContext
{
    string CorrelationId { get; }
    void SetCorrelationId(string correlationId);
}
