using FiapCloudGames.Payments.Application.Commands.ProcessPayment;
using FiapCloudGames.Payments.Application.Interfaces;
using FiapCloudGames.Queue.Contracts;
using FiapCloudGames.Queue.Publishers;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Queue.Consumers.Rabbitmq;

public class OrderPlacedConsumer(
    ILogger<OrderPlacedConsumer> logger,
    IMediator mediator,
    ICorrelationContext correlationContext,
    IPaymentProcessedPublisher paymentProcessedPublisher
) : IConsumer<IOrderPlaced>
{
    private readonly ILogger<OrderPlacedConsumer> _logger = logger;
    private readonly ICorrelationContext _correlationContext = correlationContext;

    public async Task Consume(ConsumeContext<IOrderPlaced> context)
    {
        var effectiveCorrelationId = context.CorrelationId
            ?? context.ConversationId
            ?? context.MessageId
            ?? NewId.NextGuid();

        var correlationId = effectiveCorrelationId.ToString();
        _correlationContext.SetCorrelationId(correlationId);

        try
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                "[payments-service] CorrelationId: {CorrelationId} | OrderPlacedConsumer - Received OrderId: {OrderId} (UserId: {UserId}, GameId: {GameId}, Price: {Price})",
                correlationId,
                context.Message.OrderId,
                context.Message.UserId,
                context.Message.GameId,
                context.Message.Price);
            }

            // Pagamentos hoje está modelado com OrderId como Guid.
            // Para manter compatibilidade com o contrato atual (OrderId int),
            // usamos um Correlation/OrderId interno próprio.
            var internalOrderId = Guid.NewGuid();

            var payment = await mediator.Send(
                new ProcessPaymentCommand(
                    internalOrderId,
                    context.Message.UserId,
                    context.Message.GameId,
                    context.Message.Price),
                context.CancellationToken);

            var approved = string.Equals(payment.Status, "Approved", StringComparison.OrdinalIgnoreCase);

            var paymentDate = payment.ProcessedAt.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(payment.ProcessedAt.Value, DateTimeKind.Utc))
                : DateTimeOffset.UtcNow;

            var correlationGuid = effectiveCorrelationId;

            await paymentProcessedPublisher.PublishAsync(
                orderId: context.Message.OrderId,
                amount: payment.Amount,
                paymentDate: paymentDate,
                paymentStatus: approved
                    ? FiapCloudGames.Notifications.Domain.Enums.PaymentStatus.Approved
                    : FiapCloudGames.Notifications.Domain.Enums.PaymentStatus.Rejected,
                email: context.Message.Email,
                name: context.Message.Name,
                correlationId: correlationGuid,
                cancellationToken: context.CancellationToken);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                "[payments-service] CorrelationId: {CorrelationId} | OrderPlacedConsumer - PaymentProcessed published for OrderId {OrderId}, Status {Status}",
                correlationId,
                context.Message.OrderId,
                approved ? "Approved" : "Rejected");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "[payments-service] CorrelationId: {CorrelationId} | OrderPlacedConsumer - Error processing OrderId: {OrderId}",
                correlationId,
                context.Message.OrderId);
        }
    }
}

