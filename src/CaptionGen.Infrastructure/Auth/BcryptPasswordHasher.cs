using CaptionGen.Application.Auth;
using BCryptNet = BCrypt.Net.BCrypt;

namespace CaptionGen.Infrastructure.Auth;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCryptNet.HashPassword(password);
    public bool Verify(string password, string hash) => BCryptNet.Verify(password, hash);
}
