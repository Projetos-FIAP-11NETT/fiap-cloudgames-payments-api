namespace FiapCloudGames.Payments.Domain.ValueObjects;

public sealed record TransactionId
{
    public string Value { get; }

    private TransactionId(string value)
    {
        Value = value;
    }

    public static TransactionId Generate()
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N")[..6].ToUpper();
        var value = $"TXN-{datePart}-{randomPart}";

        return new TransactionId(value);
    }

    public static TransactionId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("TransactionId não pode ser vazio", nameof(value));

        if (!IsValid(value))
            throw new ArgumentException($"TransactionId inválido: {value}", nameof(value));

        return new TransactionId(value);
    }

    private static bool IsValid(string value)
    {
        // Formato: TXN-YYYYMMDD-XXXXXX (20 caracteres)
        if (value.Length != 20)
            return false;

        if (!value.StartsWith("TXN-"))
            return false;

        var parts = value.Split('-');
        if (parts.Length != 3)
            return false;

        // Valida data (8 dígitos)
        if (parts[1].Length != 8 || !parts[1].All(char.IsDigit))
            return false;

        // Valida parte aleatória (6 caracteres alfanuméricos)
        if (parts[2].Length != 6 || !parts[2].All(char.IsLetterOrDigit))
            return false;

        return true;
    }

    public override string ToString() => Value;

    public static implicit operator string(TransactionId transactionId) => transactionId.Value;
}
