using FiapCloudGames.Notifications.Domain.Enums;

namespace FiapCloudGames.Queue.Publishers;

public interface IPaymentProcessedPublisher
{
    Task PublishAsync(
        int orderId,
        decimal amount,
        DateTimeOffset paymentDate,
        PaymentStatus paymentStatus,
        string email,
        string name,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);
}

