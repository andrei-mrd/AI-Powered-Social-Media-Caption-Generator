using System.Security.Claims;
using CaptionGen.Application.Captions;
using CaptionGen.Application.Posts;
using MediatR;
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
}

public sealed class CreatePostRequest
{
    public string Description { get; init; } = "";
    public string Platform { get; init; } = "";
    public string Tone { get; init; } = "funny";
    public string Language { get; init; } = "en";
    public string Goal { get; init; } = "awareness";
    public string CaptionLength { get; init; } = "medium";
    public bool IncludeEmojis { get; init; } = true;
    public bool IncludeCta { get; init; } = true;
    public int HashtagCount { get; init; } = 12;
    public string? Audience { get; init; }
    public string? BrandVoice { get; init; }
    public IReadOnlyList<string>? ForbiddenWords { get; init; }
    public IReadOnlyList<string>? KeywordsToInclude { get; init; }
    public int Count { get; init; } = 3;
}
