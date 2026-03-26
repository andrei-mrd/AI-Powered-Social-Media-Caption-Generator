namespace CaptionGen.Application.Auth;

public sealed record RegisterRequest(string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record MeResponse(Guid Id, string Email);