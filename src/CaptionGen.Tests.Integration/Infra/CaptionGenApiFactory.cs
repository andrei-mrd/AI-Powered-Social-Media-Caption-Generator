using System.Data.Common;
using CaptionGen.Application.Captions;
using CaptionGen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CaptionGen.Tests.Integration.Infra;

public sealed class CaptionGenApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("captiongen_test")
        .WithUsername("app")
        .WithPassword("app")
        .Build();

    public string DbConnectionString => _db.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        await using var conn = new NpgsqlConnection(DbConnectionString);
        await conn.OpenAsync();
        await EnsureDatabaseIsReady(conn);
    }

    public new async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureAppConfiguration(cfg =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Db"] = DbConnectionString,
                ["AiService:BaseUrl"] = "http://127.0.0.1:9",
                ["ASPNETCORE_URLS"] = "http://127.0.0.1:0",
                ["Jwt:Issuer"] = "CaptionGen",
                ["Jwt:Audience"] = "CaptionGen",
                ["Jwt:CookieName"] = "cg_at",
                ["Jwt:AccessMinutes"] = "60",
                ["Jwt:AllowInsecureCookieOnHttp"] = "true",
                ["Jwt:Key"] = "INTEGRATION_TEST_KEY_32CHARS_MINIMUM_1234"
            };
            cfg.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });

            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(opt => opt
                .UseNpgsql(DbConnectionString)
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

            services.RemoveAll<IAiCaptionService>();
            services.AddSingleton<IAiCaptionService, FakeAiCaptionService>();

            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        });
    }

    private static async Task EnsureDatabaseIsReady(DbConnection conn)
    {
        // Basic sanity query; container can accept connections before it's fully ready.
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1;";
        await cmd.ExecuteScalarAsync();
    }

    private sealed class FakeAiCaptionService : IAiCaptionService
    {
        public Task<CaptionGenerationResult> GenerateAsync(
            string description,
            string platform,
            string tone,
            int count,
            CaptionGenerationOptions options,
            CancellationToken ct)
        {
            var captions = Enumerable.Range(1, Math.Max(1, count))
                .Select(i => new GeneratedCaption(
                    $"{platform}:{tone}:{description}:{i}",
                    new[] { "#test", $"#tag{i}" },
                    "hook",
                    "cta",
                    80 + i))
                .ToArray();

            return Task.FromResult<CaptionGenerationResult>(
                new CaptionGenerationResult(captions, new[] { "#test" }, 80, "ok", Guid.NewGuid().ToString("N")));
        }

        public Task<CaptionImprovementResult> ImproveAsync(
            string caption,
            string platform,
            string tone,
            string language,
            string goal,
            CancellationToken cancellationToken = default)
        {
            var improved = $"improved:{caption}";
            return Task.FromResult(new CaptionImprovementResult(improved, improved[..Math.Min(improved.Length, 50)], $"{improved}:cta", Guid.NewGuid().ToString("N")));
        }
    }
}
