using FiapCloudGames.Notifications.Domain.Enums;
using FiapCloudGames.Payments.Application.Commands.ProcessPayment;
using FiapCloudGames.Payments.Application.Interfaces;
using FiapCloudGames.Queue.Contracts;
using FiapCloudGames.Queue.Publishers;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Queue.Consumers;

/// <summary>
/// Classe base que encapsula toda a lógica de consumo de OrderPlaced.
/// Subclasses apenas identificam o transport via <see cref="TransportTag"/>.
/// </summary>
public abstract class OrderPlacedConsumerBase(
    ILogger logger,
    IMediator mediator,
    ICorrelationContext correlationContext,
    IPaymentProcessedPublisher paymentProcessedPublisher
) : IConsumer<IOrderPlaced>
{
    /// <summary>
    /// Tag do transport exibida nos logs (ex.: "SQS", "RabbitMQ").
    /// </summary>
    protected abstract string TransportTag { get; }

    public async Task Consume(ConsumeContext<IOrderPlaced> context)
    {
        var effectiveCorrelationId = context.CorrelationId
            ?? context.ConversationId
            ?? context.MessageId
            ?? NewId.NextGuid();

        var correlationId = effectiveCorrelationId.ToString();
        correlationContext.SetCorrelationId(correlationId);

        try
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "[payments-service] CorrelationId: {CorrelationId} | {Transport} | OrderPlacedConsumer - Received OrderId: {OrderId} (UserId: {UserId}, GameId: {GameId}, Price: {Price})",
                    correlationId,
                    TransportTag,
                    context.Message.OrderId,
                    context.Message.UserId,
                    context.Message.GameId,
                    context.Message.Price);
            }

            var payment = await mediator.Send(
                new ProcessPaymentCommand(
                    Guid.NewGuid(),
                    context.Message.UserId,
                    context.Message.GameId,
                    context.Message.Price),
                context.CancellationToken);

            var approved = string.Equals(payment.Status, "Approved", StringComparison.OrdinalIgnoreCase);

            var paymentDate = payment.ProcessedAt.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(payment.ProcessedAt.Value, DateTimeKind.Utc))
                : DateTimeOffset.UtcNow;

            await paymentProcessedPublisher.PublishAsync(
                orderId: context.Message.OrderId,
                amount: payment.Amount,
                paymentDate: paymentDate,
                paymentStatus: approved ? PaymentStatus.Approved : PaymentStatus.Rejected,
                email: context.Message.Email,
                name: context.Message.Name,
                correlationId: effectiveCorrelationId,
                cancellationToken: context.CancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "[payments-service] CorrelationId: {CorrelationId} | {Transport} | OrderPlacedConsumer - PaymentProcessed published for OrderId {OrderId}, Status {Status}",
                    correlationId,
                    TransportTag,
                    context.Message.OrderId,
                    approved ? "Approved" : "Rejected");
            }
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "[payments-service] CorrelationId: {CorrelationId} | {Transport} | OrderPlacedConsumer - Error processing OrderId: {OrderId}",
                correlationId,
                TransportTag,
                context.Message.OrderId);
        }
    }
}