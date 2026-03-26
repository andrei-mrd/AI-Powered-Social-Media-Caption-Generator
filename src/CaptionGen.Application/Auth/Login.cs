using CaptionGen.Application.Users;
using MediatR;
using BCryptNet = BCrypt.Net.BCrypt;

namespace CaptionGen.Application.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<string>;

public sealed class LoginHandler : IRequestHandler<LoginCommand, string>
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;

    public LoginHandler(IUserRepository users, ITokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    public async Task<string> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Invalid credentials");

        var user = await _users.GetByEmailAsync(email, ct);
        if (user is null)
            throw new InvalidOperationException("Invalid credentials");

        if (!BCryptNet.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid credentials");

        return _tokens.CreateAccessToken(user);
    }
}
