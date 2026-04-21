using FiapCloudGames.Payments.Application.Interfaces;
using FiapCloudGames.Queue.Consumers.Rabbitmq;
using FiapCloudGames.Queue.Publishers;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace FiapCloudGames.Payments.Tests.Unit.Queue.Consumers.Rabbitmq;

public class OrderPlacedConsumerTests
{
    /// <summary>
    /// Garante que o TransportTag do consumidor RabbitMQ é exatamente "RabbitMQ",
    /// assegurando que os logs produzidos identificam corretamente o transport utilizado.
    /// </summary>
    [Fact]
    public void RabbitmqOrderPlacedConsumer_TransportTag_ShouldBeRabbitMQ()
    {
        // Arrange
        var consumer = new OrderPlacedConsumer(
            new Mock<ILogger<OrderPlacedConsumer>>().Object,
            new Mock<IMediator>().Object,
            new Mock<ICorrelationContext>().Object,
            new Mock<IPaymentProcessedPublisher>().Object);

        // Act
        var tag = consumer.GetTransportTagForTest();

        // Assert
        Assert.Equal("RabbitMQ", tag);
    }
}