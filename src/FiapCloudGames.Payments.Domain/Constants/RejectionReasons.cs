namespace FiapCloudGames.Payments.Domain.Constants;
public static class RejectionReasons
{
    public static readonly string[] All =
    [
        "Saldo insuficiente",
        "Cartão expirado",
        "Dados do cartão inválidos",
        "Transação negada pelo banco emissor",
        "Limite de crédito excedido",
        "Cartão bloqueado por suspeita de fraude",
        "CVV incorreto",
        "Transação acima do limite diário"
    ];

    public static string GetRandom()
    {
        var random = new Random();
        return All[random.Next(All.Length)];
    }
}