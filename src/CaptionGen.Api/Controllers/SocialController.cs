using System.Security.Claims;
using CaptionGen.Application.Social;
using CaptionGen.Infrastructure.Social;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CaptionGen.Api.Controllers;

[ApiController]
[Route("api/social")]
[Authorize]
public sealed class SocialController : ControllerBase
{
    private const string OAuthStateCookie = "linkedin_oauth_state";

    private readonly IMediator _mediator;
    private readonly ILinkedInOAuthService _linkedIn;
    private readonly ILogger<SocialController> _logger;

    public SocialController(
        IMediator mediator,
        ILinkedInOAuthService linkedIn,
        ILogger<SocialController> logger)
    {
        _mediator = mediator;
        _linkedIn = linkedIn;
        _logger = logger;
    }

    [HttpGet("connect/linkedin")]
    public IActionResult ConnectLinkedIn()
    {
        var state = Guid.NewGuid().ToString("N");

        Response.Cookies.Append(OAuthStateCookie, state, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10)
        });

        var url = _linkedIn.BuildAuthorizationUrl(state);
        return Redirect(url);
    }

    [HttpGet("callback/linkedin")]
    public async Task<IActionResult> LinkedInCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            _logger.LogWarning("LinkedIn OAuth denied by user: {Error}", error);
            return Redirect("http://localhost:5173/settings?error=linkedin_denied");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return BadRequest("Missing code or state.");

        var savedState = Request.Cookies[OAuthStateCookie];
        if (savedState != state)
            return BadRequest("Invalid OAuth state. Possible CSRF attempt.");

        Response.Cookies.Delete(OAuthStateCookie);

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        try
        {
            await _mediator.Send(new ConnectLinkedInCallbackCommand(userId, code), ct);
            return Redirect("http://localhost:5173/settings?connected=linkedin");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LinkedIn callback failed for user {UserId}", userId);
            return Redirect("http://localhost:5173/settings?error=linkedin_failed");
        }
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts(CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var accounts = await _mediator.Send(new GetSocialAccountsQuery(userId), ct);
        return Ok(accounts);
    }

    [HttpDelete("disconnect/{platform}")]
    public async Task<IActionResult> Disconnect(string platform, CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        await _mediator.Send(new DisconnectSocialAccountCommand(userId, platform.ToLowerInvariant()), ct);
        return NoContent();
    }
}
