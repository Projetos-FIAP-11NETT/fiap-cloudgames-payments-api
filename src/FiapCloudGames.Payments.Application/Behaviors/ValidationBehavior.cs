using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Payments.Application.Behaviors;
public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            _logger.LogDebug("Nenhum validador encontrado para {RequestType}", typeof(TRequest).Name);
            return await next(cancellationToken);
        }

        _logger.LogInformation("🔍 Validando requisição: {RequestType}", typeof(TRequest).Name);

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            _logger.LogWarning("❌ Validação falhou para {RequestType}: {ErrorCount} erros", typeof(TRequest).Name, failures.Count);
            throw new ValidationException(failures);
        }

        _logger.LogInformation("✅ Validação passou para {RequestType}", typeof(TRequest).Name);

        return await next(cancellationToken);
    }
}