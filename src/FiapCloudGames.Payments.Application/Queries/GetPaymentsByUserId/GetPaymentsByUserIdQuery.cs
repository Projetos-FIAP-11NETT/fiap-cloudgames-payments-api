using FiapCloudGames.Payments.Application.DTOs;
using MediatR;

namespace FiapCloudGames.Payments.Application.Queries.GetPaymentsByUserId;

public record GetPaymentsByUserIdQuery(Guid UserId) : IRequest<List<PaymentDto>>;