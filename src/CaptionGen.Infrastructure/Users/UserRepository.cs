using CaptionGen.Application.Users;
using CaptionGen.Domain.Users;
using CaptionGen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CaptionGen.Infrastructure.Users;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        => _db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }
}