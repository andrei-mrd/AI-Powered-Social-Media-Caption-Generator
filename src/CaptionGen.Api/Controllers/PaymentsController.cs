using System.Security.Claims;
using CaptionGen.Api.Contracts.Payments;
using CaptionGen.Application.Payments;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace CaptionGen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPaymentWebhookService _webhookService;

    public PaymentsController(IMediator mediator, IPaymentWebhookService webhookService)
    {
        _mediator = mediator;
        _webhookService = webhookService;
    }

    [HttpPost("checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request, CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue("sub")
                           ?? User.FindFirstValue("id");
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new CreateCheckoutSessionCommand(userId, request.PlanSlug), ct);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BuildValidationProblem(ex);
        }
        catch (InvalidOperationException ex)
        {
            const int status = StatusCodes.Status400BadRequest;
            return StatusCode(status, BuildProblem(status, "Invalid request", ex.Message));
        }
        catch (PaymentServiceException ex)
        {
            var status = ex.IsClientError
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status502BadGateway;

            return StatusCode(status, BuildProblem(status, "Payment failed", ex.Message));
        }
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue("Stripe-Signature", out StringValues signature))
        {
            return BadRequest("Missing Stripe-Signature header.");
        }

        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();

        try
        {
            await _webhookService.HandleAsync(payload, signature, ct);
            return Ok();
        }
        catch (PaymentServiceException ex)
        {
            var status = ex.IsClientError
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status500InternalServerError;
            return StatusCode(status, BuildProblem(status, "Webhook rejected", ex.Message));
        }
        catch (Exception)
        {
            const int status = StatusCodes.Status500InternalServerError;
            return StatusCode(status, BuildProblem(status, "Webhook processing failed", "Unexpected error processing webhook."));
        }
    }

    private static ProblemDetails BuildProblem(int status, string title, string detail) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail
        };

    private IActionResult BuildValidationProblem(ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => string.IsNullOrWhiteSpace(e.PropertyName) ? "General" : e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed"
        };

        return ValidationProblem(problem);
    }
}
