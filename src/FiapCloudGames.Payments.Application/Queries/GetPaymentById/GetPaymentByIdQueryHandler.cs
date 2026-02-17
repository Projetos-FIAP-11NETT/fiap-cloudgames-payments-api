using FiapCloudGames.Payments.Application.DTOs;
using FiapCloudGames.Payments.Application.Interfaces;
using FiapCloudGames.Payments.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Payments.Application.Queries.GetPaymentById;

public class GetPaymentByIdQueryHandler(
    IPaymentsRepository repository,
    ILogger<GetPaymentByIdQueryHandler> logger) : IRequestHandler<GetPaymentByIdQuery, PaymentDto?>
{
    private readonly IPaymentsRepository _repository = repository;
    private readonly ILogger<GetPaymentByIdQueryHandler> _logger = logger;

    public async Task<PaymentDto?> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔍 Buscando pagamento {PaymentId}", request.PaymentId);

        var payment = await _repository.GetByIdAsync(request.PaymentId, cancellationToken);

        if (payment == null)
        {
            _logger.LogWarning("⚠️ Pagamento {PaymentId} não encontrado", request.PaymentId);
            return null;
        }

        return MapToDto(payment);
    }

    private static PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            UserId = payment.UserId,
            GameId = payment.GameId,
            Amount = payment.Amount,
            Status = payment.Status.ToString(),
            PaymentMethod = payment.PaymentMethod?.ToString(),
            TransactionId = payment.TransactionId,
            CardLastFourDigits = payment.CardLastFourDigits,
            RejectionReason = payment.RejectionReason,
            ProcessingTimeMs = payment.ProcessingTimeMs,
            CreatedAt = payment.CreatedAt,
            ProcessedAt = payment.ProcessedAt
        };
    }
}