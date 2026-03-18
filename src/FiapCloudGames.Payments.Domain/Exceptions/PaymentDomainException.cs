using FiapCloudGames.Payments.Domain.Enums;

namespace FiapCloudGames.Payments.Domain.Exceptions;
public class PaymentDomainException : Exception
{
    public PaymentDomainException(string message) : base(message)
    {
    }

    public PaymentDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static PaymentDomainException InvalidStatus(PaymentStatus currentStatus, PaymentStatus targetStatus)
    {
        return new PaymentDomainException(
            $"Não é possível mudar pagamento de {currentStatus} para {targetStatus}");
    }

    public static PaymentDomainException InvalidAmount(decimal amount)
    {
        return new PaymentDomainException(
            $"Valor de pagamento inválido: {amount}. Deve ser maior que zero.");
    }

    public static PaymentDomainException EmptyOrderId()
    {
        return new PaymentDomainException("OrderId não pode ser vazio");
    }
}