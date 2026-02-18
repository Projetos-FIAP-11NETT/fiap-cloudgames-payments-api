using FiapCloudGames.Payments.Application.Commands.ProcessPayment;
using FiapCloudGames.Payments.Application.DTOs;
using FiapCloudGames.Payments.Application.Queries.GetPaymentById;
using FiapCloudGames.Payments.Application.Queries.GetPaymentsByUserId;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Payments.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentsController(IMediator mediator, ILogger<PaymentsController> logger) : ControllerBase
{

    /// <summary>
    /// Busca um pagamento por ID
    /// </summary>
    /// <param name="id">ID do pagamento</param>
    /// <returns>Dados do pagamento</returns>
    /// <response code="200">Pagamento encontrado</response>
    /// <response code="404">Pagamento não encontrado</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        logger.LogInformation("GET /api/payments/{PaymentId}", id);

        var payment = await mediator.Send(new GetPaymentByIdQuery(id));

        if (payment == null)
        {
            logger.LogWarning("Pagamento {PaymentId} não encontrado", id);
            return NotFound(new { message = $"Pagamento {id} não encontrado" });
        }

        return Ok(payment);
    }

    /// <summary>
    /// Lista todos os pagamentos de um usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Lista de pagamentos do usuário</returns>
    /// <response code="200">Lista de pagamentos</response>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(List<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUserId([FromRoute] Guid userId)
    {
        logger.LogInformation("GET /api/payments/user/{UserId}", userId);

        var payments = await mediator.Send(new GetPaymentsByUserIdQuery(userId));

        return Ok(new
        {
            userId,
            count = payments.Count,
            payments
        });
    }

    /// <summary>
    /// Simula um processamento de pagamento (APENAS PARA TESTES)
    /// </summary>
    /// <remarks>
    /// Em produção, este endpoint não existiria. 
    /// Pagamentos são processados via eventos (OrderPlacedEvent).
    /// Este endpoint existe apenas para testar o fluxo manualmente.
    /// </remarks>
    /// <param name="command">Dados do pagamento</param>
    /// <returns>Pagamento processado</returns>
    /// <response code="201">Pagamento criado e processado</response>
    /// <response code="400">Dados inválidos</response>
    [HttpPost("simulate")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SimulatePayment([FromBody] ProcessPaymentCommand command)
    {
        logger.LogInformation(
            "POST /api/payments/simulate - OrderId: {OrderId}, Amount: R$ {Amount:F2}",
            command.OrderId,
            command.Amount);

        try
        {
            var payment = await mediator.Send(command);

            return CreatedAtAction(
                nameof(GetById),
                new { id = payment.Id },
                payment);
        }
        catch (FluentValidation.ValidationException ex)
        {
            logger.LogWarning("Validação falhou: {Errors}", ex.Errors);

            return BadRequest(new
            {
                message = "Erro de validação",
                errors = ex.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    error = e.ErrorMessage
                })
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar pagamento");

            return BadRequest(new
            {
                message = "Erro ao processar pagamento",
                error = ex.Message
            });
        }
    }
}
