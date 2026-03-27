using System.Security.Claims;
using CaptionGen.Api.Contracts.Posts;
using CaptionGen.Application.Captions;
using CaptionGen.Application.Posts;
using MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaptionGen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PostsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PostsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue("sub")
                           ?? User.FindFirstValue("id");
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var posts = await _mediator.Send(new GetPostsQuery(userId), ct);
        return Ok(posts);
    }

    [HttpPost("{id:guid}/schedule")]
    public async Task<IActionResult> Schedule(Guid id, [FromBody] SchedulePostRequest request, CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue("sub")
                           ?? User.FindFirstValue("id");
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        try
        {
            await _mediator.Send(new SchedulePostCommand(
                userId,
                id,
                request.ScheduledAtUtc,
                request.SelectedCaptionIndex,
                request.MediaIds ?? Array.Empty<Guid>()), ct);

            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BuildValidationProblem(ex);
        }
        catch (InvalidOperationException ex)
        {
            const int status = StatusCodes.Status400BadRequest;
            return StatusCode(status, BuildProblem(status, "Schedule failed", ex.Message));
        }
    }

    [HttpPost("{id:guid}/select-caption/{variantIndex:int}")]
    public async Task<IActionResult> SelectCaption(Guid id, int variantIndex, CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue("sub")
                           ?? User.FindFirstValue("id");
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        try
        {
            await _mediator.Send(new SelectCaptionCommand(userId, id, variantIndex), ct);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BuildValidationProblem(ex);
        }
        catch (InvalidOperationException ex)
        {
            const int status = StatusCodes.Status400BadRequest;
            return StatusCode(status, BuildProblem(status, "Selection failed", ex.Message));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request, CancellationToken ct)
    {
        // Prefer "sub" claim, but accept a legacy "id" claim to keep older tokens working.
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue("sub")
                           ?? User.FindFirstValue("id");
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        try
        {
            var response = await _mediator.Send(
                new CreatePostCommand(
                    userId,
                    request.Description,
                    request.Platform,
                    request.Tone,
                    request.Language,
                    request.Goal,
                    request.CaptionLength,
                    request.IncludeEmojis,
                    request.IncludeCta,
                    request.HashtagCount,
                    request.Audience,
                    request.BrandVoice,
                    request.ForbiddenWords ?? Array.Empty<string>(),
                    request.KeywordsToInclude ?? Array.Empty<string>(),
                    request.Count),
                ct);

            return Ok(response);
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

            return StatusCode(status, BuildProblem(status, "Caption generation failed", ex.Message));
        }
        catch (HttpRequestException ex)
        {
            const int status = StatusCodes.Status502BadGateway;
            return StatusCode(status, BuildProblem(status, "Caption generation failed", ex.Message));
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
