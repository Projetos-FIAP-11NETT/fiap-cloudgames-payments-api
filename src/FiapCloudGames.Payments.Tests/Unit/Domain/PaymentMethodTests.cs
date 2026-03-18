using FiapCloudGames.Payments.Domain.Enums;
using FluentAssertions;

namespace FiapCloudGames.Payments.Tests.Unit.Domain;


public class PaymentMethodTests
{
    [Fact]
    public void PaymentMethod_Should_Have_Four_Options()
    {
        // Act
        var values = Enum.GetValues<PaymentMethod>();

        // Assert
        values.Should().HaveCount(4);
    }

    [Fact]
    public void PaymentMethod_Should_Contain_Expected_Values()
    {
        // Act & Assert
        Enum.IsDefined(PaymentMethod.CreditCard).Should().BeTrue();
        Enum.IsDefined(PaymentMethod.DebitCard).Should().BeTrue();
        Enum.IsDefined(PaymentMethod.PIX).Should().BeTrue();
        Enum.IsDefined(PaymentMethod.Boleto).Should().BeTrue();
    }

    [Theory]
    [InlineData(PaymentMethod.CreditCard, 1)]
    [InlineData(PaymentMethod.DebitCard, 2)]
    [InlineData(PaymentMethod.PIX, 3)]
    [InlineData(PaymentMethod.Boleto, 4)]
    public void PaymentMethod_Should_Have_Correct_Numeric_Values(PaymentMethod method, int expectedValue)
    {
        // Act & Assert
        ((int)method).Should().Be(expectedValue);
    }
}
