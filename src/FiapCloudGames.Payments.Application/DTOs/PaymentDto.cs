namespace FiapCloudGames.Payments.Application.DTOs;

public record PaymentDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? PaymentMethod { get; init; }
    public string? TransactionId { get; init; }
    public string? CardLastFourDigits { get; init; }
    public string? RejectionReason { get; init; }
    public int ProcessingTimeMs { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
}

