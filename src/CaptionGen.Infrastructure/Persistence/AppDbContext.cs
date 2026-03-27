using CaptionGen.Domain.Users;
using CaptionGen.Domain.Posts;
using CaptionGen.Domain.Entitlements;
using Microsoft.EntityFrameworkCore;

namespace CaptionGen.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<Caption> Captions => Set<Caption>();
    public DbSet<PostMedia> PostMedia => Set<PostMedia>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<UserEntitlement> UserEntitlements => Set<UserEntitlement>();
    public DbSet<UserUsage> UserUsages => Set<UserUsage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
            b.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            b.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Post>(b =>
        {
            b.ToTable("posts");
            b.HasKey(x => x.Id);

            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();

            b.Property(x => x.Description).HasColumnName("description").HasMaxLength(4000).IsRequired();
            b.Property(x => x.Platform).HasColumnName("platform").HasMaxLength(32).IsRequired();
            b.Property(x => x.Tone).HasColumnName("tone").HasMaxLength(32).IsRequired();
            b.Property(x => x.Language).HasColumnName("language").HasMaxLength(8).IsRequired();
            b.Property(x => x.Goal).HasColumnName("goal").HasMaxLength(32).IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(24).IsRequired();

            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            b.Property(x => x.ScheduledAtUtc).HasColumnName("scheduled_at_utc");

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.UserId, x.Status });
            b.HasIndex(x => x.ScheduledAtUtc);
            
            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MediaAsset>(b =>
        {
            b.ToTable("media_assets");
            b.HasKey(x => x.Id);

            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();

            b.Property(x => x.Type).HasColumnName("type").HasMaxLength(16).IsRequired();
            b.Property(x => x.StoragePath).HasColumnName("storage_path").HasMaxLength(1024).IsRequired();
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.CreatedAtUtc);
            
            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Caption>(b =>
        {
            b.ToTable("captions");
            b.HasKey(x => x.Id);

            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.PostId).HasColumnName("post_id").IsRequired();

            b.Property(x => x.VariantIndex).HasColumnName("variant_index").IsRequired();
            b.Property(x => x.Text).HasColumnName("text").HasMaxLength(4000).IsRequired();
            b.Property(x => x.HashtagsText).HasColumnName("hashtags_text").HasMaxLength(2000);
            b.Property(x => x.Hook).HasColumnName("hook").HasMaxLength(500);
            b.Property(x => x.Cta).HasColumnName("cta").HasMaxLength(500);
            b.Property(x => x.IsSelected).HasColumnName("is_selected").HasDefaultValue(false).IsRequired();
            b.Property(x => x.Score).HasColumnName("score");
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

            b.HasIndex(x => x.PostId);
            b.HasIndex(x => new { x.PostId, x.VariantIndex }).IsUnique();

            b.HasOne(x => x.Post)
                .WithMany(x => x.Captions)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostMedia>(b =>
        {
            b.ToTable("post_media");
            b.HasKey(x => new { x.PostId, x.MediaAssetId });

            b.Property(x => x.PostId).HasColumnName("post_id");
            b.Property(x => x.MediaAssetId).HasColumnName("media_asset_id");

            b.HasIndex(x => x.PostId);
            b.HasIndex(x => x.MediaAssetId);

            b.HasOne(x => x.Post)
                .WithMany(x => x.PostMedia)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.MediaAsset)
                .WithMany(x => x.PostMedia)
                .HasForeignKey(x => x.MediaAssetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Plan>(b =>
        {
            b.ToTable("plans");
            b.HasKey(x => x.Id);

            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(64).IsRequired();
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
            b.Property(x => x.CaptionGenerationsPerMonth).HasColumnName("caption_generations_per_month").IsRequired();
            b.Property(x => x.MediaAssetsLimit).HasColumnName("media_assets_limit").IsRequired();
            b.Property(x => x.SeatsIncluded).HasColumnName("seats_included").IsRequired();
            b.Property(x => x.SchedulingEnabled).HasColumnName("scheduling_enabled").IsRequired();
            b.Property(x => x.AiImproveEnabled).HasColumnName("ai_improve_enabled").IsRequired();
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

            b.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<UserEntitlement>(b =>
        {
            b.ToTable("user_entitlements");
            b.HasKey(x => x.Id);

            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            b.Property(x => x.PlanId).HasColumnName("plan_id").IsRequired();
            b.Property(x => x.ActiveUntilUtc).HasColumnName("active_until_utc");
            b.Property(x => x.SeatsInUse).HasColumnName("seats_in_use").IsRequired();
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

            b.HasIndex(x => x.UserId).IsUnique();
            b.HasIndex(x => x.PlanId);

            b.HasOne(x => x.Plan)
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserUsage>(b =>
        {
            b.ToTable("user_usage");
            b.HasKey(x => x.Id);

            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            b.Property(x => x.PeriodStartUtc).HasColumnName("period_start_utc").IsRequired();
            b.Property(x => x.CaptionsUsed).HasColumnName("captions_used").IsRequired();
            b.Property(x => x.MediaUsed).HasColumnName("media_used").IsRequired();
            b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

            b.HasIndex(x => new { x.UserId, x.PeriodStartUtc }).IsUnique();

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
