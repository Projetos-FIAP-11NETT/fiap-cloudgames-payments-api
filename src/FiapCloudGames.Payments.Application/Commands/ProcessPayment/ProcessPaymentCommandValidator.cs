using FluentValidation;

namespace FiapCloudGames.Payments.Application.Commands.ProcessPayment;
public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("OrderId é obrigatório");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId é obrigatório");

        RuleFor(x => x.GameId)
            .NotEmpty()
            .WithMessage("GameId é obrigatório");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount deve ser maior que zero");
    }
}