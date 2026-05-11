using System.Security.Claims;
using CaptionGen.Application.Auth;
using CaptionGen.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CaptionGen.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, IOptions<JwtOptions> jwtOptions, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        try
        {
            var id = await _mediator.Send(new RegisterCommand(req.Email, req.Password), ct);
            return Ok(new { id });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed for {Email}", req.Email);
            var status = ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase)
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;

            return StatusCode(status, BuildProblem(status, "Registration failed", ex.Message));
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        try
        {
            var token = await _mediator.Send(new LoginCommand(req.Email, req.Password), ct);

            Response.Cookies.Append(_jwtOptions.CookieName, token, BuildCookieOptions(addExpiry: true));

            return Ok(new { token });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Login failed for {Email}", req.Email);
            const int status = StatusCodes.Status401Unauthorized;
            return StatusCode(status, BuildProblem(status, "Login failed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login exception for {Email}", req.Email);
            const int status = StatusCodes.Status500InternalServerError;
            return StatusCode(status, BuildProblem(status, "Login failed", "Unexpected error, please try again."));
        }
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType<LogoutResponse>(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(_jwtOptions.CookieName, BuildCookieOptions());
        return Ok(new LogoutResponse(true));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (id is null || email is null) return Unauthorized();
        return Ok(new MeResponse(Guid.Parse(id), email));
    }

    private static ProblemDetails BuildProblem(int status, string title, string detail) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail
        };

    private CookieOptions BuildCookieOptions(bool addExpiry = false)
    {
        var allowInsecure = _jwtOptions.AllowInsecureCookieOnHttp;
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = !allowInsecure,
            SameSite = allowInsecure ? SameSiteMode.Lax : SameSiteMode.None,
            Path = "/"
        };

        if (addExpiry)
        {
            options.Expires = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.AccessMinutes);
        }

        return options;
    }
}

public sealed record LogoutResponse(bool Ok);
