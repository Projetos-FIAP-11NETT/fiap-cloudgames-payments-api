using FiapCloudGames.Payments.Domain.Entities;
using FiapCloudGames.Payments.Domain.Enums;
using FiapCloudGames.Payments.Domain.ValueObjects;
using FluentAssertions;
namespace FiapCloudGames.Payments.Tests.Unit.Domain;

public class PaymentTests
{
    private readonly Guid _orderId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _gameId = Guid.NewGuid();
    private const decimal Amount = 199.90m;

    [Fact]
    public void Create_Should_Create_Payment_With_Pending_Status()
    {
        // Act
        var payment = Payment.Create(_orderId, _userId, _gameId, Amount);

        // Assert
        payment.Should().NotBeNull();
        payment.Id.Should().NotBeEmpty();
        payment.OrderId.Should().Be(_orderId);
        payment.UserId.Should().Be(_userId);
        payment.GameId.Should().Be(_gameId);
        payment.Amount.Should().Be(Amount);
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        payment.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void Create_Should_Throw_When_OrderId_Is_Empty()
    {
        // Act
        var act = () => Payment.Create(Guid.Empty, _userId, _gameId, Amount);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*OrderId*");
    }

    [Fact]
    public void Create_Should_Throw_When_UserId_Is_Empty()
    {
        // Act
        var act = () => Payment.Create(_orderId, Guid.Empty, _gameId, Amount);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*UserId*");
    }

    [Fact]
    public void Create_Should_Throw_When_GameId_Is_Empty()
    {
        // Act
        var act = () => Payment.Create(_orderId, _userId, Guid.Empty, Amount);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*GameId*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Create_Should_Throw_When_Amount_Is_Zero_Or_Negative(decimal amount)
    {
        // Act
        var act = () => Payment.Create(_orderId, _userId, _gameId, amount);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*maior que zero*");
    }

    [Fact]
    public void StartProcessing_Should_Change_Status_To_Processing()
    {
        // Arrange
        var payment = Payment.Create(_orderId, _userId, _gameId, Amount);

        // Act
        payment.StartProcessing();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Processing);
    }

    [Fact]
    public void StartProcessing_Should_Throw_When_Status_Is_Not_Pending()
    {
        // Arrange
        var payment = Payment.Create(_orderId, _userId, _gameId, Amount);
        payment.StartProcessing();

        // Act
        var act = () => payment.StartProcessing();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*processar pagamento com status*");
    }

    [Fact]
    public void Approve_Should_Set_Payment_As_Approved_With_Transaction_Details()
    {
        // Arrange
        var payment = Payment.Create(_orderId, _userId, _gameId, Amount);
        payment.StartProcessing();
        var transactionId = TransactionId.Generate();
        const string cardDigits = "4532";
        const int processingTime = 1247;

        // Act
        payment.Approve(transactionId, PaymentMethod.CreditCard, cardDigits, processingTime);

        // Assert
        payment.Status.Should().Be(PaymentStatus.Approved);
        payment.TransactionId.Should().Be(transactionId.Value);
        payment.PaymentMethod.Should().Be(PaymentMethod.CreditCard);
        payment.CardLastFourDigits.Should().Be(cardDigits);
        payment.ProcessingTimeMs.Should().Be(processingTime);
        payment.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        payment.IsApproved().Should().BeTrue();
        payment.IsProcessed().Should().BeTrue();
    }

    [Fact]
    public void Approve_Should_Throw_When_Status_Is_Not_Processing()
    {
        // Arrange
        var payment = Payment.Create(_orderId, _userId, _gameId, Amount);
        var transactionId = TransactionId.Generate();

        // Act
        var act = () => payment.Approve(transactionId, PaymentMethod.CreditCard, "4532", 1000);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*aprovar pagamento com status*");
    }

    [Fact]
    public void Approve_Should_Throw_When_TransactionId_Is_Empty()
    {
        // Arrange
        var payment = Payment.Create(_orderId, _userId, _gameId, Amount);
        payment.StartProcessing();

        // Act
        var act = () => payment.Approve("", PaymentMethod.CreditCard, "4532", 1000);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*TransactionId*");
    }

    [Fact]
    public void Reject_Should_Set_Payment_As_Rejected_With_Reason()
    {
        // Arrange
        var payment = Payment.Create(_orderId, _userId, _gameId, Amount);
        payment.StartProcessing();
        const string rejectionReason = "Saldo insuficiente";
        const int processingTime = 891;

        // Act
        payment.Reject(rejectionReason, processingTime);

        // Assert
        payment.Status.Should().Be(PaymentStatus.Rejected);
        payment.RejectionReason.Should().Be(rejectionReason);
        payment.ProcessingTimeMs.Should().Be(processingTime);
        payment.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        payment.IsRejected().Should().BeTrue();
        payment.IsProcessed().Should().BeTrue();
    }

    [Fact]
    public void Reject_Should_Throw_When_Status_Is_Not_Processing()
    {
        // Arrange
        var payment = Payment.Create(_orderId, _userId, _gameId, Amount);

        // Act
        var act = () => payment.Reject("Motivo qualquer", 1000);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*rejeitar pagamento com status*");
    }

    [Fact]
    public void Reject_Should_Throw_When_RejectionReason_Is_Empty()
    {
        // Arrange
        var payment = Payment.Create(_orderId, _userId, _gameId, Amount);
        payment.StartProcessing();

        // Act
        var act = () => payment.Reject("", 1000);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*RejectionReason*");
    }

    [Fact]
    public void IsApproved_Should_Return_False_When_Not_Approved()
    {
        // Arrange
        var payment = Payment.Create(_orderId, _userId, _gameId, Amount);

        // Act & Assert
        payment.IsApproved().Should().BeFalse();
    }

    [Fact]
    public void IsRejected_Should_Return_False_When_Not_Rejected()
    {
        // Arrange
        var payment = Payment.Create(_orderId, _userId, _gameId, Amount);

        // Act & Assert
        payment.IsRejected().Should().BeFalse();
    }

    [Fact]
    public void IsProcessed_Should_Return_False_When_Pending_Or_Processing()
    {
        // Arrange
        var payment = Payment.Create(_orderId, _userId, _gameId, Amount);

        // Act & Assert
        payment.IsProcessed().Should().BeFalse();

        payment.StartProcessing();
        payment.IsProcessed().Should().BeFalse();
    }
}
