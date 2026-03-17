using System.Text.Json;

namespace FiapCloudGames.Payments.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private const string CorrelationIdHeader = "x-correlation-id";
    private const string CorrelationIdItemKey = "CorrelationId";
    private const string MessageException = "[payments-service] CorrelationId: {CorrelationId} | Exceção Capturada | Message: {Message}";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var correlationId = GetCorrelationId(context);

            logger.LogError(ex, MessageException, correlationId, ex.Message);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                context.Response.Headers[CorrelationIdHeader] = correlationId;

                var response = JsonSerializer.Serialize(new
                {
                    message = "Erro interno no servidor.",
                    correlationId
                });

                await context.Response.WriteAsync(response);
            }
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationIdItemKey, out var correlationId)
            && correlationId is string correlationIdValue
            && !string.IsNullOrWhiteSpace(correlationIdValue))
        {
            return correlationIdValue;
        }

        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerCorrelationId)
            && !string.IsNullOrWhiteSpace(headerCorrelationId))
        {
            return headerCorrelationId.ToString();
        }

        return context.TraceIdentifier;
    }
}
