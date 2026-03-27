using System.Security.Claims;
using CaptionGen.Application.Entitlements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaptionGen.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class EntitlementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EntitlementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue("sub")
                           ?? User.FindFirstValue("id");
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var entitlements = await _mediator.Send(new GetEntitlementsQuery(userId), ct);
        return Ok(entitlements);
    }
}
