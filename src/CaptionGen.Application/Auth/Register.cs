using CaptionGen.Application.Users;
using CaptionGen.Application.Entitlements;
using CaptionGen.Domain.Users;
using MediatR;
using BCryptNet = BCrypt.Net.BCrypt;

namespace CaptionGen.Application.Auth;

public sealed record RegisterCommand(string Email, string Password) : IRequest<Guid>;

public sealed class RegisterHandler : IRequestHandler<RegisterCommand, Guid>
{
    private readonly IUserRepository _users;
    private readonly IEntitlementService _entitlements;

    public RegisterHandler(IUserRepository users, IEntitlementService entitlements)
    {
        _users = users;
        _entitlements = entitlements;
    }

    public async Task<Guid> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Invalid credentials");

        var existing = await _users.GetByEmailAsync(email, ct);
        if (existing is not null)
            throw new InvalidOperationException("Email already used");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCryptNet.HashPassword(request.Password),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _users.AddAsync(user, ct);
        await _entitlements.AssignPlanAsync(user.Id, "basic", ct);
        return user.Id;
    }
}
