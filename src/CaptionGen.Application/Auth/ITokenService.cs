using CaptionGen.Domain.Users;

namespace CaptionGen.Application.Auth;

public interface ITokenService
{
    string CreateAccessToken(User user);
}