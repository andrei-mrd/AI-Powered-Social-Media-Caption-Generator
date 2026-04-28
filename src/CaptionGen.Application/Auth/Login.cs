using CaptionGen.Application.Users;
using MediatR;

namespace CaptionGen.Application.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<string>;

public sealed class LoginHandler : IRequestHandler<LoginCommand, string>
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly IPasswordHasher _passwordHasher;

    public LoginHandler(IUserRepository users, ITokenService tokens, IPasswordHasher passwordHasher)
    {
        _users = users;
        _tokens = tokens;
        _passwordHasher = passwordHasher;
    }

    public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Invalid credentials");

        var user = await _users.GetByEmailAsync(email, cancellationToken);
        if (user is null)
            throw new InvalidOperationException("Invalid credentials");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid credentials");

        return _tokens.CreateAccessToken(user);
    }
}
