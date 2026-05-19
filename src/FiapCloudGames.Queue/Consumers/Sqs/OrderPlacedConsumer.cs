using FiapCloudGames.Payments.Application.Interfaces;
using FiapCloudGames.Queue.Publishers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Queue.Consumers.Sqs;

public sealed class OrderPlacedConsumer(
    ILogger<OrderPlacedConsumer> logger,
    IMediator mediator,
    ICorrelationContext correlationContext,
    IPaymentProcessedPublisher paymentProcessedPublisher,
    IEmailNotificationPublisher emailNotificationPublisher
) : OrderPlacedConsumerBase(logger, mediator, correlationContext, paymentProcessedPublisher, emailNotificationPublisher)
{
    protected override string TransportTag => "SQS";
}