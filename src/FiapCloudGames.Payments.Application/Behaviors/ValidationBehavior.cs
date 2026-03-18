using FiapCloudGames.Payments.Application.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Payments.Application.Behaviors;
public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger,
    ICorrelationContext correlationContext) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger = logger;
    private readonly ICorrelationContext _correlationContext = correlationContext;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            _logger.LogDebug("[payments-service] CorrelationId: {CorrelationId} | Nenhum validador encontrado para {RequestType}", _correlationContext.CorrelationId, typeof(TRequest).Name);
            return await next(cancellationToken);
        }

        _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | 🔍 Validando requisição: {RequestType}", _correlationContext.CorrelationId, typeof(TRequest).Name);

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            _logger.LogWarning("[payments-service] CorrelationId: {CorrelationId} | ❌ Validação falhou para {RequestType}: {ErrorCount} erros", _correlationContext.CorrelationId, typeof(TRequest).Name, failures.Count);
            throw new ValidationException(failures);
        }

        _logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | ✅ Validação passou para {RequestType}", _correlationContext.CorrelationId, typeof(TRequest).Name);

        return await next(cancellationToken);
    }
}