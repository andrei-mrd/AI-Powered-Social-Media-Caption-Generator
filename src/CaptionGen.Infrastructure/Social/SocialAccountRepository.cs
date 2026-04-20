using CaptionGen.Application.Social;
using CaptionGen.Domain.Social;
using CaptionGen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CaptionGen.Infrastructure.Social;

public sealed class SocialAccountRepository : ISocialAccountRepository
{
    private readonly AppDbContext _db;

    public SocialAccountRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SocialAccount>> GetByUserAsync(Guid userId, CancellationToken ct)
        => await _db.SocialAccounts
            .Where(a => a.UserId == userId)
            .ToListAsync(ct);

    public Task<SocialAccount?> GetByUserAndPlatformAsync(Guid userId, string platform, CancellationToken ct)
        => _db.SocialAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Platform == platform, ct);

    public async Task UpsertAsync(SocialAccount account, CancellationToken ct)
    {
        var existing = await _db.SocialAccounts
            .FirstOrDefaultAsync(a => a.UserId == account.UserId && a.Platform == account.Platform, ct);

        if (existing is null)
            _db.SocialAccounts.Add(account);
        else
        {
            existing.PlatformUserId = account.PlatformUserId;
            existing.DisplayName = account.DisplayName;
            existing.AccessTokenEncrypted = account.AccessTokenEncrypted;
            existing.RefreshTokenEncrypted = account.RefreshTokenEncrypted;
            existing.TokenExpiresAtUtc = account.TokenExpiresAtUtc;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid userId, string platform, CancellationToken ct)
    {
        var account = await _db.SocialAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Platform == platform, ct);

        if (account is not null)
        {
            _db.SocialAccounts.Remove(account);
            await _db.SaveChangesAsync(ct);
        }
    }
}
