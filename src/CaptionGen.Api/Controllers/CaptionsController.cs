using CaptionGen.Api.Contracts.Captions;
using CaptionGen.Application.Captions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaptionGen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CaptionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CaptionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("improve")]
    public async Task<IActionResult> Improve(
        [FromBody] ImproveCaptionRequestDto request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new ImproveCaptionCommand(
                    request.Caption,
                    request.Platform,
                    request.Tone,
                    request.Language,
                    request.Goal),
                ct);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            const int status = StatusCodes.Status400BadRequest;
            return StatusCode(status, BuildProblem(status, "Invalid request", ex.Message));
        }
        catch (ValidationException ex)
        {
            return BuildValidationProblem(ex);
        }
        catch (AiServiceException ex)
        {
            var status = ex.IsClientError
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status502BadGateway;

            return StatusCode(status, BuildProblem(status, "Improve caption failed", ex.Message));
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
