using FiapCloudGames.Payments.Application.DTOs;
using MediatR;

namespace FiapCloudGames.Payments.Application.Commands.ProcessPayment;

public record ProcessPaymentCommand(
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Amount
) : IRequest<PaymentDto>;