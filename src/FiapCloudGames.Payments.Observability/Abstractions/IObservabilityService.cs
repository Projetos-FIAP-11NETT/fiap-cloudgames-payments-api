namespace FiapCloudGames.Payments.Observability.Abstractions;

public interface IObservabilityService
{
    void AddCustomAttribute(string key, object value);
    void NoticeError(Exception exception);
    void SetTransactionName(string category, string name);
}
