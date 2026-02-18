using FiapCloudGames.Payments.Domain.Enums;

namespace FiapCloudGames.Payments.Domain.Entities;

public class Payment
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid GameId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public PaymentMethod? PaymentMethod { get; private set; }
    public string? TransactionId { get; private set; }
    public string? CardLastFourDigits { get; private set; }
    public string? RejectionReason { get; private set; }
    public int ProcessingTimeMs { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private Payment() { }

    public static Payment Create(Guid orderId, Guid userId, Guid gameId, decimal amount)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId não pode ser vazio", nameof(orderId));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId não pode ser vazio", nameof(userId));

        if (gameId == Guid.Empty)
            throw new ArgumentException("GameId não pode ser vazio", nameof(gameId));

        if (amount <= 0)
            throw new ArgumentException("Amount deve ser maior que zero", nameof(amount));

        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            UserId = userId,
            GameId = gameId,
            Amount = amount,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void StartProcessing()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Não é possível processar pagamento com status {Status}");

        Status = PaymentStatus.Processing;
    }

    public void Approve(
        string transactionId,
        PaymentMethod paymentMethod,
        string? cardLastFourDigits,
        int processingTimeMs)
    {
        if (Status != PaymentStatus.Processing)
            throw new InvalidOperationException($"Não é possível aprovar pagamento com status {Status}");

        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentException("TransactionId é obrigatório", nameof(transactionId));

        Status = PaymentStatus.Approved;
        TransactionId = transactionId;
        PaymentMethod = paymentMethod;
        CardLastFourDigits = cardLastFourDigits;
        ProcessingTimeMs = processingTimeMs;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject(string rejectionReason, int processingTimeMs)
    {
        if (Status != PaymentStatus.Processing)
            throw new InvalidOperationException($"Não é possível rejeitar pagamento com status {Status}");

        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("RejectionReason é obrigatório", nameof(rejectionReason));

        Status = PaymentStatus.Rejected;
        RejectionReason = rejectionReason;
        ProcessingTimeMs = processingTimeMs;
        ProcessedAt = DateTime.UtcNow;
    }

    public bool IsApproved() => Status == PaymentStatus.Approved;
    public bool IsRejected() => Status == PaymentStatus.Rejected;
    public bool IsProcessed() => Status == PaymentStatus.Approved || Status == PaymentStatus.Rejected;
}
