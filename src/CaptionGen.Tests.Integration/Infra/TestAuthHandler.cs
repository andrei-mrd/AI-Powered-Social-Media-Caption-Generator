using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CaptionGen.Tests.Integration.Infra;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var id = Request.Headers.TryGetValue("X-Test-UserId", out var idValues) && Guid.TryParse(idValues.ToString(), out var parsed)
            ? parsed.ToString()
            : "11111111-1111-1111-1111-111111111111";

        var email = Request.Headers.TryGetValue("X-Test-Email", out var emailValues) && !string.IsNullOrWhiteSpace(emailValues.ToString())
            ? emailValues.ToString()!
            : "integration@test.local";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id),
            new Claim(ClaimTypes.Email, email),
            new Claim("sub", id),
            new Claim("email", email)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

