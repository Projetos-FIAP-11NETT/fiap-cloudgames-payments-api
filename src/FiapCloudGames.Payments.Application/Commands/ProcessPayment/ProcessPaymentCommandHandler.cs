using FiapCloudGames.Payments.Application.DTOs;
using FiapCloudGames.Payments.Application.Interfaces;
using FiapCloudGames.Payments.Domain.Entities;
using FiapCloudGames.Payments.Domain.Enums;
using FiapCloudGames.Payments.Domain.ValueObjects;
using FiapCloudGames.Payments.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace FiapCloudGames.Payments.Application.Commands.ProcessPayment;

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, PaymentDto>
{
    private readonly IPaymentsRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;
    private readonly ICorrelationContext _correlationContext;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly Random _random = new();

    public ProcessPaymentCommandHandler(
        IPaymentsRepository repository,
        IConfiguration configuration,
        ILogger<ProcessPaymentCommandHandler> logger,
        ICorrelationContext correlationContext)
    {
        _repository = repository;
        _configuration = configuration;
        _logger = logger;
        _correlationContext = correlationContext;

        // Configurar Polly Retry Policy
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), // 2s, 4s, 8s
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "[payments-service] CorrelationId: {CorrelationId} | 🔄 Tentativa {RetryCount}/3 após falha. Aguardando {Seconds}s. Erro: {Message}",
                        _correlationContext.CorrelationId,
                        retryCount,
                        timeSpan.TotalSeconds,
                        exception.Message);
                });
    }

    public async Task<PaymentDto> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[payments-service] CorrelationId: {CorrelationId} | 💳 Iniciando processamento de pagamento - OrderId: {OrderId}, Amount: R$ {Amount:F2}",
            _correlationContext.CorrelationId,
            request.OrderId,
            request.Amount);

        // Criar pagamento com Polly retry
        var payment = await _retryPolicy.ExecuteAsync(async () =>
        {
            // 1. Criar entidade Payment
            var newPayment = Payment.Create(
                request.OrderId,
                request.UserId,
                request.GameId,
                request.Amount);

            // 2. Salvar como Pending
            await _repository.AddAsync(newPayment, cancellationToken);

            return newPayment;
        });

        // 3. Iniciar processamento
        payment.StartProcessing();
        await _repository.UpdateAsync(payment, cancellationToken);

        // 4. Simular processamento (delay)
        var processingTimeMs = _random.Next(500, 2000);
        await Task.Delay(processingTimeMs, cancellationToken);

        // 5. Decidir aprovação ou rejeição
        double approvalRate = 0.9; // valor padrão
        try
        {
            var paymentSection = _configuration.GetSection("Payment");
            var approvalRateValue = paymentSection?.GetSection("ApprovalRate")?.Value;

            if (!string.IsNullOrWhiteSpace(approvalRateValue) &&
                double.TryParse(approvalRateValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                approvalRate = parsed;
            }
        }
        catch
        {
            // Em caso de qualquer problema ao acessar a configuração, manter o fallback 0.9
        }

        var isApproved = _random.NextDouble() < approvalRate;

        if (isApproved)
        {
            // ✅ APROVAR PAGAMENTO
            var transactionId = TransactionId.Generate();
            var paymentMethod = GetRandomPaymentMethod();
            var cardDigits = (paymentMethod == PaymentMethod.CreditCard ||
                                     paymentMethod == PaymentMethod.DebitCard)
                        ? GenerateRandomCardDigits()
                        : null;

            payment.Approve(transactionId, paymentMethod, cardDigits, processingTimeMs);

            await _repository.UpdateAsync(payment, cancellationToken);

            _logger.LogInformation(@"[payments-service] CorrelationId: {CorrelationId} |
╔══════════════════════════════════════════════════════════════╗
║                   ✅ PAGAMENTO APROVADO ✅                    ║
╚══════════════════════════════════════════════════════════════╝
PaymentId: {PaymentId}
OrderId: {OrderId}
Valor: R$ {Amount:F2}
Método: {PaymentMethod}
Cartão: **** **** **** {CardDigits}
Transaction ID: {TransactionId}
Tempo: {ProcessingTime}ms
Status: APROVADO
╚══════════════════════════════════════════════════════════════╝",
                _correlationContext.CorrelationId,
                payment.Id,
                payment.OrderId,
                payment.Amount,
                payment.PaymentMethod,
                payment.CardLastFourDigits,
                payment.TransactionId,
                payment.ProcessingTimeMs);
        }
        else
        {
            // ❌ REJEITAR PAGAMENTO
            var rejectionReason = RejectionReasons.GetRandom();

            payment.Reject(rejectionReason, processingTimeMs);

            await _repository.UpdateAsync(payment, cancellationToken);

            _logger.LogWarning(@"[payments-service] CorrelationId: {CorrelationId} |
╔══════════════════════════════════════════════════════════════╗
║                   ❌ PAGAMENTO REJEITADO ❌                   ║
╚══════════════════════════════════════════════════════════════╝
PaymentId: {PaymentId}
OrderId: {OrderId}
Valor: R$ {Amount:F2}
Motivo: {RejectionReason}
Tempo: {ProcessingTime}ms
Status: REJEITADO
╚══════════════════════════════════════════════════════════════╝",
                _correlationContext.CorrelationId,
                payment.Id,
                payment.OrderId,
                payment.Amount,
                payment.RejectionReason,
                payment.ProcessingTimeMs);
        }

        // 6. Retornar DTO
        return MapToDto(payment);
    }

    private PaymentMethod GetRandomPaymentMethod()
    {
        var methods = Enum.GetValues<PaymentMethod>();
        return methods[_random.Next(methods.Length)];
    }

    private string GenerateRandomCardDigits()
    {
        return _random.Next(1000, 9999).ToString();
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
