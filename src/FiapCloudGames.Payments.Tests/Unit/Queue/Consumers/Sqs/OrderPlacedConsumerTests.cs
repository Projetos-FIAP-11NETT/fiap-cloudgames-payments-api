using FiapCloudGames.Payments.Application.Interfaces;
using FiapCloudGames.Queue.Consumers.Sqs;
using FiapCloudGames.Queue.Publishers;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace FiapCloudGames.Payments.Tests.Unit.Queue.Consumers.Sqs;

public class OrderPlacedConsumerTests
{
    /// <summary>
    /// Garante que o TransportTag do consumidor SQS � exatamente "SQS",
    /// assegurando que os logs produzidos identificam corretamente o transport utilizado.
    /// </summary>
    [Fact]
    public void SqsOrderPlacedConsumer_TransportTag_ShouldBeSqs()
    {
        // Arrange
        var consumer = new OrderPlacedConsumer(
            new Mock<ILogger<OrderPlacedConsumer>>().Object,
            new Mock<IMediator>().Object,
            new Mock<ICorrelationContext>().Object,
            new Mock<IPaymentProcessedPublisher>().Object,
            new Mock<IEmailNotificationPublisher>().Object);

        // Act
        var tag = consumer.GetTransportTagForTest();

        // Assert
        Assert.Equal("SQS", tag);
    }
}