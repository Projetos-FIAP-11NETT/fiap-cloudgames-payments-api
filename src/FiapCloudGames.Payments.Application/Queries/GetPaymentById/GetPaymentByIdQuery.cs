using FiapCloudGames.Payments.Application.DTOs;
using MediatR;

namespace FiapCloudGames.Payments.Application.Queries.GetPaymentById;
public record GetPaymentByIdQuery(Guid PaymentId) : IRequest<PaymentDto?>;