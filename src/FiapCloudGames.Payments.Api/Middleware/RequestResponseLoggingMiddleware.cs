using System.Diagnostics;

namespace FiapCloudGames.Payments.Api.Middleware;

public class RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
{
    private const string CorrelationIdHeader = "x-correlation-id";
    private const string CorrelationIdItemKey = "CorrelationId";
    private const string MessageRequest = "[user-service] CorrelationId: {CorrelationId} | Inicio da Requisicao {Method} {Path}";
    private const string MessageResponse = "[user-service] CorrelationId: {CorrelationId} | Final da Requisicao {Method} {Path} | StatusCode: {StatusCode} {Elapsed}ms";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        var stopwatch = Stopwatch.StartNew();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(MessageRequest, correlationId, context.Request.Method, context.Request.Path);
        }

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                MessageResponse,
                correlationId,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
            }
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerCorrelationId)
            && !string.IsNullOrWhiteSpace(headerCorrelationId))
        {
            var correlationIdFromHeader = headerCorrelationId.ToString();
            context.Response.Headers[CorrelationIdHeader] = correlationIdFromHeader;
            context.Items[CorrelationIdItemKey] = correlationIdFromHeader;
            return correlationIdFromHeader;
        }

        var correlationId = context.TraceIdentifier;
        context.Response.Headers[CorrelationIdHeader] = correlationId;
        context.Items[CorrelationIdItemKey] = correlationId;
        return correlationId;
    }
}
