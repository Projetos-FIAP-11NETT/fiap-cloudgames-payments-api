using FiapCloudGames.Payments.Api.Constants;
using FiapCloudGames.Payments.Api.Sessions;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using System.Text.Json;

namespace FiapCloudGames.Payments.Api.Middleware;

public class SessionValidationMiddleware
    (
        RequestDelegate next,
        IDistributedCache distributedCache,
        IConfiguration configuration,
        ILogger<SessionValidationMiddleware> logger
    )
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderNames.SessionId, out var sessionHeader) ||
            !Guid.TryParse(sessionHeader, out var sessionId))
        {
            await RejectAsync(context, "Sessao nao informada ou invalida.");
            return;
        }

        var session = await GetSessionAsync(sessionId, context.RequestAborted);
        if (session is null)
        {
            await RejectAsync(context, "Sessao expirada ou inexistente.");
            return;
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            await RejectAsync(context, "Sessao expirada.");
            return;
        }

        var tokenEmail = GetTokenEmail(context.User);
        if (!string.IsNullOrWhiteSpace(tokenEmail) &&
            !string.Equals(tokenEmail, session.Email, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Session {SessionId} email {SessionEmail} does not match token email {TokenEmail}",
                sessionId,
                session.Email,
                tokenEmail);

            await RejectAsync(context, "Sessao nao pertence ao usuario autenticado.");
            return;
        }

        context.Items[ContextItems.SessionId] = session.SessionId;
        context.Items[ContextItems.SessionEmail] = session.Email;

        await next(context);
    }

    private async Task<SessionCacheEntry?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var keyPrefix = configuration["SessionCache:KeyPrefix"] ?? "session:";
        var json = await distributedCache.GetStringAsync($"{keyPrefix}{sessionId}", cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<SessionCacheEntry>(json, SerializerOptions);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Could not deserialize session {SessionId} from distributed cache", sessionId);
            return null;
        }
    }

    private static string? GetTokenEmail(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue("email");
    }

    private static async Task RejectAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            error = message
        });
    }
}
