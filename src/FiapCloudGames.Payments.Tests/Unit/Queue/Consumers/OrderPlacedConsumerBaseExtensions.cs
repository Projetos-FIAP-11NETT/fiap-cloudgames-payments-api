using FiapCloudGames.Queue.Consumers;

namespace FiapCloudGames.Payments.Tests.Unit.Queue.Consumers;

/// <summary>
/// Extensão exclusiva para testes, expondo propriedades protegidas da classe base.
/// </summary>
internal static class OrderPlacedConsumerBaseExtensions
{
    internal static string GetTransportTagForTest(this OrderPlacedConsumerBase consumer)
    {
        var prop = typeof(OrderPlacedConsumerBase)
            .GetProperty("TransportTag",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        return (string)prop!.GetValue(consumer)!;
    }
}