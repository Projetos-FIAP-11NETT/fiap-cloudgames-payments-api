using FiapCloudGames.Notifications.Domain.Enums;
using FiapCloudGames.Queue.Contracts;
using FiapCloudGames.Queue.Configurations.Rabbitmq;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Queue.Publishers;

public class PaymentProcessedPublisher(IRabbitmqPublish bus, ILogger<PaymentProcessedPublisher> logger) : IPaymentProcessedPublisher
{
    private readonly IPublishEndpoint _publishEndpoint = bus;
    private readonly ILogger<PaymentProcessedPublisher> _logger = logger;

    public Task PublishAsync(
        int orderId,
        decimal amount,
        DateTimeOffset paymentDate,
        PaymentStatus paymentStatus,
        string email,
        string name,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Publishing IPaymentProcessed to RabbitMQ: OrderId={OrderId}, Amount={Amount}, Status={Status}",
            orderId, amount, paymentStatus);

        return _publishEndpoint.Publish<IPaymentProcessed>(new
        {
            OrderId = orderId,
            Amount = amount,
            PaymentDate = paymentDate,
            PaymentStatus = paymentStatus,
            Email = email,
            Name = name
        },
        context =>
        {
            if (correlationId.HasValue)
            {
                context.CorrelationId = correlationId.Value;
            }
        },
        cancellationToken);
    }
}

