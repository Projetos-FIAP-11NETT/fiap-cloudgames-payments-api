using FiapCloudGames.Payments.Application.DTOs;
using FiapCloudGames.Payments.Application.Interfaces;
using FiapCloudGames.Payments.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Payments.Application.Queries.GetPaymentsByUserId;
public class GetPaymentsByUserIdQueryHandler(
    IPaymentsRepository repository,
    ILogger<GetPaymentsByUserIdQueryHandler> logger,
    ICorrelationContext correlationContext) : IRequestHandler<GetPaymentsByUserIdQuery, List<PaymentDto>>
{
    private readonly IPaymentsRepository _repository = repository;
    private readonly ILogger<GetPaymentsByUserIdQueryHandler> _logger = logger;
    private readonly ICorrelationContext _correlationContext = correlationContext;

    public async Task<List<PaymentDto>> Handle(GetPaymentsByUserIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | 🔍 Buscando pagamentos do usuário {UserId}", _correlationContext.CorrelationId, request.UserId);

        var payments = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);

        _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | ✅ Encontrados {Count} pagamentos para o usuário {UserId}",
            _correlationContext.CorrelationId,
            payments.Count,
            request.UserId);

        return [.. payments.Select(MapToDto)];
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