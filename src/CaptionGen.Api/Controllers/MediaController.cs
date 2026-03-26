using System.Security.Claims;
using CaptionGen.Application.Media;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaptionGen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MediaController : ControllerBase
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB
    private readonly IMediator _mediator;

    public MediaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var items = await _mediator.Send(new ListMediaQuery(userId.Value), ct);
        return Ok(items);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        if (file is null || file.Length == 0)
            return BadRequest(BuildProblem(StatusCodes.Status400BadRequest, "Invalid file", "No file provided."));

        await using var stream = file.OpenReadStream();

        try
        {
            var result = await _mediator.Send(
                new UploadMediaCommand(userId.Value, file.FileName, file.ContentType, file.Length, stream),
                ct);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            const int status = StatusCodes.Status400BadRequest;
            return StatusCode(status, BuildProblem(status, "Upload failed", ex.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        try
        {
            await _mediator.Send(new DeleteMediaCommand(id, userId.Value), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            const int status = StatusCodes.Status404NotFound;
            return StatusCode(status, BuildProblem(status, "Delete failed", ex.Message));
        }
    }

    /// <summary>
    /// Local placeholder for presigned uploads; returns the existing upload endpoint and expiry.
    /// </summary>
    [HttpPost("presign")]
    public IActionResult Presign()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        return Ok(new
        {
            uploadUrl = Url.ActionLink(nameof(Upload), values: new { controller = "Media" }),
            expiresAt
        });
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue("sub")
                           ?? User.FindFirstValue("id");
        return Guid.TryParse(userIdValue, out var id) ? id : null;
    }

    private static ProblemDetails BuildProblem(int status, string title, string detail) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail
        };
}
