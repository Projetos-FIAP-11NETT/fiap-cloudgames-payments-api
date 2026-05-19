using FiapCloudGames.Notifications.Domain.Enums;
using FiapCloudGames.Payments.Application.Commands.ProcessPayment;
using FiapCloudGames.Payments.Application.DTOs;
using FiapCloudGames.Payments.Application.Interfaces;
using FiapCloudGames.Queue.Consumers;
using FiapCloudGames.Queue.Contracts;
using FiapCloudGames.Queue.Publishers;
using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace FiapCloudGames.Payments.Tests.Unit.Queue.Consumers;

/// <summary>
/// Subclasse concreta criada exclusivamente para testes da classe base,
/// expondo um TransportTag control�vel pelo teste.
/// </summary>
file sealed class TestOrderPlacedConsumer(
    ILogger logger,
    IMediator mediator,
    ICorrelationContext correlationContext,
    IPaymentProcessedPublisher paymentProcessedPublisher,
    IEmailNotificationPublisher emailNotificationPublisher,
    string transportTag = "TEST"
) : OrderPlacedConsumerBase(logger, mediator, correlationContext, paymentProcessedPublisher, emailNotificationPublisher)
{
    protected override string TransportTag { get; } = transportTag;
}

public class OrderPlacedConsumerBaseTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ICorrelationContext> _correlationContextMock;
    private readonly Mock<IPaymentProcessedPublisher> _publisherMock;
    private readonly Mock<IEmailNotificationPublisher> _emailPublisherMock;
    private readonly OrderPlacedConsumerBase _consumer;

    public OrderPlacedConsumerBaseTests()
    {
        _loggerMock = new Mock<ILogger>();
        _mediatorMock = new Mock<IMediator>();
        _correlationContextMock = new Mock<ICorrelationContext>();
        _publisherMock = new Mock<IPaymentProcessedPublisher>();
        _emailPublisherMock = new Mock<IEmailNotificationPublisher>();

        _consumer = new TestOrderPlacedConsumer(
            _loggerMock.Object,
            _mediatorMock.Object,
            _correlationContextMock.Object,
            _publisherMock.Object,
            _emailPublisherMock.Object);
    }

    /// <summary>
    /// Cria um ConsumeContext mockado com os dados fornecidos,
    /// evitando duplica��o de c�digo nos testes.
    /// </summary>
    private static Mock<ConsumeContext<IOrderPlaced>> BuildConsumeContext(
        int orderId = 42,
        Guid? userId = null,
        Guid? gameId = null,
        decimal price = 99.99m,
        string email = "user@test.com",
        string name = "Test User",
        Guid? correlationId = null)
    {
        var message = new Mock<IOrderPlaced>();
        message.Setup(m => m.OrderId).Returns(orderId);
        message.Setup(m => m.UserId).Returns(userId ?? Guid.NewGuid());
        message.Setup(m => m.GameId).Returns(gameId ?? Guid.NewGuid());
        message.Setup(m => m.Price).Returns(price);
        message.Setup(m => m.Email).Returns(email);
        message.Setup(m => m.Name).Returns(name);

        var context = new Mock<ConsumeContext<IOrderPlaced>>();
        context.Setup(c => c.Message).Returns(message.Object);
        context.Setup(c => c.CorrelationId).Returns(correlationId ?? Guid.NewGuid());
        context.Setup(c => c.ConversationId).Returns((Guid?)null);
        context.Setup(c => c.MessageId).Returns((Guid?)null);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        return context;
    }

    /// <summary>
    /// Cria um PaymentDto com status e data de processamento configur�veis.
    /// </summary>
    private static PaymentDto BuildPaymentDto(string status = "Approved", DateTime? processedAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Amount = 99.99m,
            Status = status,
            ProcessedAt = processedAt ?? DateTime.UtcNow
        };

    /// <summary>
    /// Garante que, quando o mediator retorna status "Approved",
    /// o publisher � chamado com PaymentStatus.Approved.
    /// </summary>
    [Fact]
    public async Task Consume_WhenPaymentApproved_ShouldPublishWithApprovedStatus()
    {
        // Arrange
        var context = BuildConsumeContext();
        var paymentDto = BuildPaymentDto("Approved");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentDto);

        // Act
        await _consumer.Consume(context.Object);

        // Assert
        _publisherMock.Verify(p => p.PublishAsync(
            It.IsAny<int>(),
            paymentDto.Amount,
            It.IsAny<DateTimeOffset>(),
            PaymentStatus.Approved,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Garante que, quando o mediator retorna status "Rejected",
    /// o publisher � chamado com PaymentStatus.Rejected.
    /// </summary>
    [Fact]
    public async Task Consume_WhenPaymentRejected_ShouldPublishWithRejectedStatus()
    {
        // Arrange
        var context = BuildConsumeContext();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPaymentDto("Rejected"));

        // Act
        await _consumer.Consume(context.Object);

        // Assert
        _publisherMock.Verify(p => p.PublishAsync(
            It.IsAny<int>(),
            It.IsAny<decimal>(),
            It.IsAny<DateTimeOffset>(),
            PaymentStatus.Rejected,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Confirma que o CorrelationId do contexto � propagado ao ICorrelationContext,
    /// garantindo rastreabilidade distribu�da entre os servi�os.
    /// </summary>
    [Fact]
    public async Task Consume_ShouldSetCorrelationIdFromContext()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var context = BuildConsumeContext(correlationId: correlationId);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPaymentDto());

        // Act
        await _consumer.Consume(context.Object);

        // Assert
        _correlationContextMock.Verify(
            c => c.SetCorrelationId(correlationId.ToString()),
            Times.Once);
    }

    /// <summary>
    /// Valida que, quando CorrelationId � nulo, o consumidor usa ConversationId como fallback,
    /// respeitando a hierarquia de IDs definida na implementa��o.
    /// </summary>
    [Fact]
    public async Task Consume_WhenCorrelationIdIsNull_ShouldFallbackToConversationId()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var message = new Mock<IOrderPlaced>();
        message.Setup(m => m.OrderId).Returns(1);
        message.Setup(m => m.UserId).Returns(Guid.NewGuid());
        message.Setup(m => m.GameId).Returns(Guid.NewGuid());
        message.Setup(m => m.Price).Returns(10m);
        message.Setup(m => m.Email).Returns("a@b.com");
        message.Setup(m => m.Name).Returns("Name");

        var context = new Mock<ConsumeContext<IOrderPlaced>>();
        context.Setup(c => c.Message).Returns(message.Object);
        context.Setup(c => c.CorrelationId).Returns((Guid?)null);
        context.Setup(c => c.ConversationId).Returns(conversationId);
        context.Setup(c => c.MessageId).Returns((Guid?)null);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPaymentDto());

        // Act
        await _consumer.Consume(context.Object);

        // Assert
        _correlationContextMock.Verify(
            c => c.SetCorrelationId(conversationId.ToString()),
            Times.Once);
    }

    /// <summary>
    /// Assegura que, quando PaymentDto.ProcessedAt � nulo,
    /// o consumidor usa DateTimeOffset.UtcNow como data do pagamento.
    /// </summary>
    [Fact]
    public async Task Consume_WhenProcessedAtIsNull_ShouldUseUtcNowAsPaymentDate()
    {
        // Arrange
        var context = BuildConsumeContext();
        var paymentDto = BuildPaymentDto() with { ProcessedAt = null };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentDto);

        var before = DateTimeOffset.UtcNow;

        // Act
        await _consumer.Consume(context.Object);

        var after = DateTimeOffset.UtcNow;

        // Assert
        _publisherMock.Verify(p => p.PublishAsync(
            It.IsAny<int>(),
            It.IsAny<decimal>(),
            It.Is<DateTimeOffset>(d => d >= before && d <= after),
            It.IsAny<PaymentStatus>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifica se o ProcessPaymentCommand enviado ao mediator cont�m
    /// exatamente os dados recebidos na mensagem (UserId, GameId e Price).
    /// </summary>
    [Fact]
    public async Task Consume_ShouldSendProcessPaymentCommandWithMessageData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var context = BuildConsumeContext(userId: userId, gameId: gameId, price: 149.90m);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPaymentDto());

        // Act
        await _consumer.Consume(context.Object);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<ProcessPaymentCommand>(cmd =>
                cmd.UserId == userId &&
                cmd.GameId == gameId &&
                cmd.Amount == 149.90m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Confirma que os campos de notifica��o (OrderId, Email e Name)
    /// s�o repassados ao publisher exatamente como recebidos na mensagem.
    /// </summary>
    [Fact]
    public async Task Consume_ShouldPublishWithCorrectOrderIdEmailAndName()
    {
        // Arrange
        const int orderId = 123;
        const string email = "gamer@test.com";
        const string name = "Cloud Gamer";
        var context = BuildConsumeContext(orderId: orderId, email: email, name: name);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPaymentDto());

        // Act
        await _consumer.Consume(context.Object);

        // Assert
        _publisherMock.Verify(p => p.PublishAsync(
            orderId,
            It.IsAny<decimal>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<PaymentStatus>(),
            email,
            name,
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Garante que a compara��o do status "Approved" � case-insensitive,
    /// conforme o uso de StringComparison.OrdinalIgnoreCase na implementa��o.
    /// </summary>
    [Theory]
    [InlineData("approved")]
    [InlineData("APPROVED")]
    [InlineData("Approved")]
    public async Task Consume_WhenStatusIsApprovedCaseInsensitive_ShouldPublishApprovedStatus(string status)
    {
        // Arrange
        var context = BuildConsumeContext();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPaymentDto(status));

        // Act
        await _consumer.Consume(context.Object);

        // Assert
        _publisherMock.Verify(p => p.PublishAsync(
            It.IsAny<int>(),
            It.IsAny<decimal>(),
            It.IsAny<DateTimeOffset>(),
            PaymentStatus.Approved,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Cobre o tratamento de erros: quando o mediator lan�a uma exce��o,
    /// o consumidor n�o deve propagar o erro, mas deve registrar via LogError com o OrderId.
    /// </summary>
    [Fact]
    public async Task Consume_WhenMediatorThrows_ShouldLogErrorAndNotThrow()
    {
        // Arrange
        var context = BuildConsumeContext(orderId: 99);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Payment gateway unavailable"));

        // Act
        var act = async () => await _consumer.Consume(context.Object);

        // Assert
        await act.Should().NotThrowAsync();

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("99")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Valida que uma falha no publisher tamb�m � capturada pelo bloco catch,
    /// registrando o erro sem propagar a exce��o para o MassTransit.
    /// </summary>
    [Fact]
    public async Task Consume_WhenPublisherThrows_ShouldLogErrorAndNotThrow()
    {
        // Arrange
        var context = BuildConsumeContext(orderId: 77);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPaymentDto());

        _publisherMock
            .Setup(p => p.PublishAsync(
                It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<PaymentStatus>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Broker unavailable"));

        // Act
        var act = async () => await _consumer.Consume(context.Object);

        // Assert
        await act.Should().NotThrowAsync();

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("77")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}