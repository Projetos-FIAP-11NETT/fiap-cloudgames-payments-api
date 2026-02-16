using FiapCloudGames.Payments.Domain.Entities;

namespace FiapCloudGames.Payments.Application.Interfaces;
public interface IPaymentsRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<List<Payment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<bool> ExistsByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
}