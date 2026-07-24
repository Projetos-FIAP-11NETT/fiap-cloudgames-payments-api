namespace FiapCloudGames.Payments.Api.Sessions;

public sealed class SessionCacheEntry
{
    public Guid SessionId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
