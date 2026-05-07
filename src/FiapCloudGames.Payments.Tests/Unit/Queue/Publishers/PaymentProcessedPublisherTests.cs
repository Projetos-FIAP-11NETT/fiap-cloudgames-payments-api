using FiapCloudGames.Notifications.Domain.Enums;
using FiapCloudGames.Queue.Configurations.Sqs;
using FiapCloudGames.Queue.Contracts;
using FiapCloudGames.Queue.Publishers;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace FiapCloudGames.Payments.Tests.Unit.Queue.Publishers;

public class PaymentProcessedPublisherTests
{
    private readonly Mock<ISqsPublish> _busMock;
    private readonly Mock<ILogger<PaymentProcessedPublisher>> _loggerMock;
    private readonly PaymentProcessedPublisher _publisher;

    public PaymentProcessedPublisherTests()
    {
        _busMock = new Mock<ISqsPublish>();
        _loggerMock = new Mock<ILogger<PaymentProcessedPublisher>>();

        _publisher = new PaymentProcessedPublisher(
            _busMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Método auxiliar que configura o mock do bus para o método de interface real do MassTransit.
    /// </summary>
    private void SetupBusPublish(Action<object>? onPublish = null)
    {
        _busMock
            .Setup(b => b.Publish<IPaymentProcessed>(
                It.IsAny<object>(),
                It.IsAny<IPipe<PublishContext<IPaymentProcessed>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<object, IPipe<PublishContext<IPaymentProcessed>>, CancellationToken>(
                (msg, _, _) => onPublish?.Invoke(msg))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Lê uma propriedade de um objeto anônimo via reflexão.
    /// Necessário pois o publisher utiliza new { } na chamada ao endpoint.
    /// </summary>
    private static T GetProp<T>(object obj, string name) =>
        (T)obj.GetType().GetProperty(name)!.GetValue(obj)!;

    /// <summary>
    /// Garante que PublishAsync delega a publicação ao IPublishEndpoint com os dados corretos,
    /// confirmando que nenhuma transformação indevida ocorre nos valores antes do envio.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ShouldPublishIPaymentProcessedWithCorrectData()
    {
        // Arrange
        const int orderId = 10;
        const decimal amount = 199.90m;
        var paymentDate = DateTimeOffset.UtcNow;
        const PaymentStatus status = PaymentStatus.Approved;
        const string email = "gamer@test.com";
        const string name = "Cloud Gamer";
        var correlationId = Guid.NewGuid();

        object? capturedMsg = null;
        SetupBusPublish(msg => capturedMsg = msg);

        // Act
        await _publisher.PublishAsync(orderId, amount, paymentDate, status, email, name, correlationId);

        // Assert
        _busMock.Verify(b => b.Publish<IPaymentProcessed>(
            It.IsAny<object>(),
            It.IsAny<IPipe<PublishContext<IPaymentProcessed>>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        capturedMsg.Should().NotBeNull();
        GetProp<int>(capturedMsg!, "OrderId").Should().Be(orderId);
        GetProp<decimal>(capturedMsg!, "Amount").Should().Be(amount);
        GetProp<DateTimeOffset>(capturedMsg!, "PaymentDate").Should().Be(paymentDate);
        GetProp<PaymentStatus>(capturedMsg!, "PaymentStatus").Should().Be(status);
        GetProp<string>(capturedMsg!, "Email").Should().Be(email);
        GetProp<string>(capturedMsg!, "Name").Should().Be(name);
    }

    /// <summary>
    /// Verifica que o PublishAsync funciona corretamente mesmo sem correlationId,
    /// garantindo que o parâmetro opcional não quebra o fluxo de publicação.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WhenCorrelationIdIsNull_ShouldPublishSuccessfully()
    {
        // Arrange
        SetupBusPublish();

        // Act
        var act = async () => await _publisher.PublishAsync(
            orderId: 1,
            amount: 50m,
            paymentDate: DateTimeOffset.UtcNow,
            paymentStatus: PaymentStatus.Approved,
            email: "a@b.com",
            name: "Name",
            correlationId: null);

        // Assert
        await act.Should().NotThrowAsync();

        _busMock.Verify(b => b.Publish<IPaymentProcessed>(
            It.IsAny<object>(),
            It.IsAny<IPipe<PublishContext<IPaymentProcessed>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Assegura que o PublishAsync publica corretamente com PaymentStatus.Rejected,
    /// validando que o status de rejeição não altera o comportamento do método.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WhenStatusIsRejected_ShouldPublishWithRejectedStatus()
    {
        // Arrange
        object? capturedMsg = null;
        SetupBusPublish(msg => capturedMsg = msg);

        // Act
        await _publisher.PublishAsync(
            orderId: 2,
            amount: 75m,
            paymentDate: DateTimeOffset.UtcNow,
            paymentStatus: PaymentStatus.Rejected,
            email: "b@c.com",
            name: "Name");

        // Assert
        GetProp<PaymentStatus>(capturedMsg!, "PaymentStatus").Should().Be(PaymentStatus.Rejected);
    }

    /// <summary>
    /// Confirma que o CancellationToken é repassado ao endpoint de publicação,
    /// permitindo o cancelamento correto da operação em cenários de timeout ou shutdown.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ShouldForwardCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        CancellationToken capturedToken = default;

        _busMock
            .Setup(b => b.Publish<IPaymentProcessed>(
                It.IsAny<object>(),
                It.IsAny<IPipe<PublishContext<IPaymentProcessed>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<object, IPipe<PublishContext<IPaymentProcessed>>, CancellationToken>(
                (_, _, ct) => capturedToken = ct)
            .Returns(Task.CompletedTask);

        // Act
        await _publisher.PublishAsync(
            orderId: 3,
            amount: 30m,
            paymentDate: DateTimeOffset.UtcNow,
            paymentStatus: PaymentStatus.Approved,
            email: "c@d.com",
            name: "Name",
            cancellationToken: token);

        // Assert
        capturedToken.Should().Be(token);
    }

    /// <summary>
    /// Garante que uma exceção lançada pelo endpoint de publicação é propagada ao chamador,
    /// permitindo que a camada superior (consumidor) decida como tratar o erro.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WhenBusThrows_ShouldPropagateException()
    {
        // Arrange
        _busMock
            .Setup(b => b.Publish<IPaymentProcessed>(
                It.IsAny<object>(),
                It.IsAny<IPipe<PublishContext<IPaymentProcessed>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SQS unavailable"));

        // Act
        var act = async () => await _publisher.PublishAsync(
            orderId: 99,
            amount: 10m,
            paymentDate: DateTimeOffset.UtcNow,
            paymentStatus: PaymentStatus.Approved,
            email: "x@y.com",
            name: "Name");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SQS unavailable");
    }

    /// <summary>
    /// Verifica que o LogInformation é chamado com correlationId e orderId,
    /// garantindo rastreabilidade da operação nos logs do serviço.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ShouldLogInformationBeforePublishing()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        SetupBusPublish();

        // Act
        await _publisher.PublishAsync(
            orderId: 5,
            amount: 59.90m,
            paymentDate: DateTimeOffset.UtcNow,
            paymentStatus: PaymentStatus.Approved,
            email: "log@test.com",
            name: "Logger Test",
            correlationId: correlationId);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains(correlationId.ToString()) &&
                    v.ToString()!.Contains("5")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}