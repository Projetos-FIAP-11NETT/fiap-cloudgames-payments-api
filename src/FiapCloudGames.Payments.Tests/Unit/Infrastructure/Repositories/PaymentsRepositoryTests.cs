using FiapCloudGames.Payments.Application.Interfaces;
using FiapCloudGames.Payments.Domain.Entities;
using FiapCloudGames.Payments.Domain.ValueObjects;
using FiapCloudGames.Payments.Domain.Enums;
using FiapCloudGames.Payments.Infrastructure.Data;
using FiapCloudGames.Payments.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Payments.Tests.Unit.Infrastructure.Repositories;

public class PaymentsRepositoryTests : IDisposable
{
    private readonly PaymentsDbContext _context;
    private readonly PaymentsRepository _repository;

    private sealed class NullCorrelationContext : ICorrelationContext
    {
        public string CorrelationId => "n/a";
        public void SetCorrelationId(string correlationId) { }
    }

    public PaymentsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PaymentsDbContext(options);
        var logger = new Logger<PaymentsRepository>(new LoggerFactory());
        _repository = new PaymentsRepository(_context, logger, new NullCorrelationContext());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPayment_WhenExists()
    {
        // Arrange
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100.00m);
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(payment.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(payment.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddPayment()
    {
        // Arrange
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50.00m);

        // Act
        var result = await _repository.AddAsync(payment);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(payment.Id);
        _context.Payments.Should().Contain(p => p.Id == payment.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdatePayment()
    {
        // Arrange
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100.00m);
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        payment.StartProcessing();

        // Act
        await _repository.UpdateAsync(payment);

        // Assert
        var updated = await _context.Payments.FindAsync(payment.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(PaymentStatus.Processing);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnPayments()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var payment1 = Payment.Create(Guid.NewGuid(), userId, Guid.NewGuid(), 100.00m);
        var payment2 = Payment.Create(Guid.NewGuid(), userId, Guid.NewGuid(), 200.00m);
        await _context.Payments.AddRangeAsync(payment1, payment2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByUserIdAsync(userId);

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(p => p.Id == payment1.Id);
        results.Should().Contain(p => p.Id == payment2.Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnEmptyList_WhenUserHasNoPayments()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var results = await _repository.GetByUserIdAsync(userId);

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ExistsByTransactionIdAsync_ShouldReturnTrue_WhenExists()
    {
        // Arrange
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100.00m);
        payment.StartProcessing();
        payment.Approve(TransactionId.Generate(), PaymentMethod.CreditCard, "1234", 100);
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsByTransactionIdAsync(payment.TransactionId!);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByTransactionIdAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Act
        var result = await _repository.ExistsByTransactionIdAsync("NONEXISTENT");

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}