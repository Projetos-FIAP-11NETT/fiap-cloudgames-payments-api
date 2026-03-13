using FiapCloudGames.Payments.Observability.Abstractions;
using Microsoft.AspNetCore.Http;

namespace FiapCloudGames.Payments.Observability.Middleware;

public class ObservabilityMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "x-correlation-id";

    public async Task InvokeAsync(HttpContext context, IObservabilityService obs)
    {
        var correlationId = GetCorrelationId(context);

        obs.AddCustomAttribute("TraceId", context.TraceIdentifier);
        obs.AddCustomAttribute("CorrelationId", correlationId);

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            obs.NoticeError(ex);
            throw;
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerCorrelationId)
            && !string.IsNullOrWhiteSpace(headerCorrelationId))
        {
            return headerCorrelationId.ToString();
        }

        return context.TraceIdentifier;
    }
}
