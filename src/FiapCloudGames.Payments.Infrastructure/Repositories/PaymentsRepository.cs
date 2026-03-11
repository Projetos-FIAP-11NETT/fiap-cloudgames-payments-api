using FiapCloudGames.Payments.Application.Interfaces;
using FiapCloudGames.Payments.Domain.Entities;
using FiapCloudGames.Payments.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Payments.Infrastructure.Repositories;

public class PaymentsRepository : IPaymentsRepository
{
    private readonly PaymentsDbContext _context;
    private readonly ILogger<PaymentsRepository> _logger;

    public PaymentsRepository(PaymentsDbContext context, ILogger<PaymentsRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 Buscando pagamento por ID: {PaymentId}", id);

        var payment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (payment == null)
        {
            _logger.LogWarning("⚠️ Pagamento {PaymentId} não encontrado", id);
        }
        else
        {
            _logger.LogInformation("✅ Pagamento {PaymentId} encontrado", id);
        }

        return payment;
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 Buscando pagamento por OrderId: {OrderId}", orderId);

        var payment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);

        if (payment == null)
        {
            _logger.LogWarning("⚠️ Pagamento para OrderId {OrderId} não encontrado", orderId);
        }
        else
        {
            _logger.LogInformation("✅ Pagamento para OrderId {OrderId} encontrado: {PaymentId}", orderId, payment.Id);
        }

        return payment;
    }

    public async Task<List<Payment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 Buscando pagamentos do usuário: {UserId}", userId);

        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("✅ Encontrados {Count} pagamentos para o usuário {UserId}", payments.Count, userId);

        return payments;
    }

    public async Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➕ Adicionando novo pagamento: {PaymentId}, OrderId: {OrderId}", payment.Id, payment.OrderId);

        await _context.Payments.AddAsync(payment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ Pagamento {PaymentId} adicionado com sucesso", payment.Id);

        return payment;
    }

    public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔄 Atualizando pagamento: {PaymentId}, Status: {Status}", payment.Id, payment.Status);

        _context.Payments.Update(payment);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ Pagamento {PaymentId} atualizado com sucesso", payment.Id);
    }

    public async Task<bool> ExistsByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 Verificando existência de TransactionId: {TransactionId}", transactionId);

        var exists = await _context.Payments
            .AsNoTracking()
            .AnyAsync(p => p.TransactionId == transactionId, cancellationToken);

        _logger.LogInformation("✅ TransactionId {TransactionId} existe: {Exists}", transactionId, exists);

        return exists;
    }
}