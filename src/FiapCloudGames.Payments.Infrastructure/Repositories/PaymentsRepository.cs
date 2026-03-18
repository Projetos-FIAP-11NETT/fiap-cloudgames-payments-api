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
    private readonly ICorrelationContext _correlationContext;

    public PaymentsRepository(PaymentsDbContext context, ILogger<PaymentsRepository> logger, ICorrelationContext correlationContext)
    {
        _context = context;
        _logger = logger;
        _correlationContext = correlationContext;
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | 🔍 Buscando pagamento por ID: {PaymentId}", _correlationContext.CorrelationId, id);

        var payment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (payment == null)
        {
            _logger.LogWarning("[payments-service] CorrelationId: {CorrelationId} | ⚠️ Pagamento {PaymentId} não encontrado", _correlationContext.CorrelationId, id);
        }
        else
        {
            _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | ✅ Pagamento {PaymentId} encontrado", _correlationContext.CorrelationId, id);
        }

        return payment;
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | 🔍 Buscando pagamento por OrderId: {OrderId}", _correlationContext.CorrelationId, orderId);

        var payment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);

        if (payment == null)
        {
            _logger.LogWarning("[payments-service] CorrelationId: {CorrelationId} | ⚠️ Pagamento para OrderId {OrderId} não encontrado", _correlationContext.CorrelationId, orderId);
        }
        else
        {
            _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | ✅ Pagamento para OrderId {OrderId} encontrado: {PaymentId}", _correlationContext.CorrelationId, orderId, payment.Id);
        }

        return payment;
    }

    public async Task<List<Payment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | 🔍 Buscando pagamentos do usuário: {UserId}", _correlationContext.CorrelationId, userId);

        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | ✅ Encontrados {Count} pagamentos para o usuário {UserId}", _correlationContext.CorrelationId, payments.Count, userId);

        return payments;
    }

    public async Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | ➕ Adicionando novo pagamento: {PaymentId}, OrderId: {OrderId}", _correlationContext.CorrelationId, payment.Id, payment.OrderId);

        await _context.Payments.AddAsync(payment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | ✅ Pagamento {PaymentId} adicionado com sucesso", _correlationContext.CorrelationId, payment.Id);

        return payment;
    }

    public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | 🔄 Atualizando pagamento: {PaymentId}, Status: {Status}", _correlationContext.CorrelationId, payment.Id, payment.Status);

        _context.Payments.Update(payment);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | ✅ Pagamento {PaymentId} atualizado com sucesso", _correlationContext.CorrelationId, payment.Id);
    }

    public async Task<bool> ExistsByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | 🔍 Verificando existência de TransactionId: {TransactionId}", _correlationContext.CorrelationId, transactionId);

        var exists = await _context.Payments
            .AsNoTracking()
            .AnyAsync(p => p.TransactionId == transactionId, cancellationToken);

        _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | ✅ TransactionId {TransactionId} existe: {Exists}", _correlationContext.CorrelationId, transactionId, exists);

        return exists;
    }
}
