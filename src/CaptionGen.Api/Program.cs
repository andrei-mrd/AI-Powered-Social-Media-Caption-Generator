using System.Text;
using CaptionGen.Application.Auth;
using CaptionGen.Application.Captions;
using CaptionGen.Application.Posts;
using CaptionGen.Application.Media;
using CaptionGen.Application.Users;
using CaptionGen.Application.Entitlements;
using CaptionGen.Application.Payments;
using CaptionGen.Infrastructure.Auth;
using CaptionGen.Infrastructure.Captions;
using CaptionGen.Infrastructure.Entitlements;
using CaptionGen.Infrastructure.Media;
using CaptionGen.Infrastructure.Payments;
using CaptionGen.Infrastructure.Persistence;
using CaptionGen.Infrastructure.Posts;
using CaptionGen.Infrastructure.Users;
using CaptionGen.Application.Common.Validation;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Stripe;

static void LoadEnvFiles()
{
    var candidates = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(AppContext.BaseDirectory, ".env"),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env")),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env"))
    };

    foreach (var path in candidates.Distinct())
    {
        if (!System.IO.File.Exists(path)) continue;

        foreach (var line in System.IO.File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
            var separatorIndex = trimmed.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0) continue;

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(key)) continue;

            Environment.SetEnvironmentVariable(key, value);
        }

        break;
    }
}

LoadEnvFiles();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CaptionGen API",
        Version = "v1"
    });
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Db")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
builder.Services.AddScoped<IMediaStorageService, LocalMediaStorageService>();
builder.Services.AddHostedService<ScheduledPostWorker>();
builder.Services.AddScoped<IEntitlementService, EntitlementService>();
builder.Services.AddScoped<IUsageService, UsageService>();
builder.Services.AddScoped<IPaymentService, StripePaymentService>();
builder.Services.AddScoped<IPaymentWebhookService, StripeWebhookService>();

builder.Services.Configure<AiServiceOptions>(
    builder.Configuration.GetSection(AiServiceOptions.SectionName));
builder.Services.Configure<MediaStorageOptions>(
    builder.Configuration.GetSection(MediaStorageOptions.SectionName));
builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection(StripeOptions.SectionName));

builder.Services.AddHttpClient("AiService.Health", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<AiServiceOptions>>().Value;
    if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri))
    {
        throw new InvalidOperationException("AiService:BaseUrl is not a valid absolute URI.");
    }

    client.BaseAddress = uri;
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 120));
});

builder.Services.AddHttpClient<IAiCaptionService, AiCaptionClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<AiServiceOptions>>().Value;
    if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri))
    {
        throw new InvalidOperationException("AiService:BaseUrl is not a valid absolute URI.");
    }

    client.BaseAddress = uri;
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 120));
});

builder.Services.AddSingleton<IStripeClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<StripeOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.SecretKey))
    {
        throw new InvalidOperationException("Stripe:SecretKey is not configured.");
    }

    return new StripeClient(options.SecretKey);
});

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly));
builder.Services.AddValidatorsFromAssemblyContaining<CreatePostCommandValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database")
    .AddCheck<AiServiceHealthCheck>("ai_service");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ??
    new[]
    {
        "https://localhost:5173",
        "http://localhost:5173",
        "https://localhost:5003",
        "http://localhost:5003",
        "http://localhost:5012",
        "https://localhost:7280",
        "https://localhost:5012"
    };

var jwtKey = builder.Configuration["Jwt:Key"]!;
var issuer = builder.Configuration["Jwt:Issuer"]!;
var audience = builder.Configuration["Jwt:Audience"]!;
var cookieName = builder.Configuration["Jwt:CookieName"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(10)
        };

        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                if (ctx.Request.Cookies.TryGetValue(cookieName, out var token))
                    ctx.Token = token;

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("wasm", p =>
        p.WithOrigins(allowedOrigins)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});

var app = builder.Build();

await DbInitializer.InitializeAsync(app.Services, app.Configuration, app.Logger);

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error is not null)
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("GlobalException");
            logger.LogError(feature.Error, "Unhandled exception");
        }

        var origin = context.Request.Headers["Origin"].ToString();
        if (!string.IsNullOrWhiteSpace(origin) &&
            allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred. Please try again."
        };

        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.UseSwagger();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/swagger/v1/swagger.json", "CaptionGen API v1");
    opt.RoutePrefix = "docs";
});

var urls = builder.Configuration["ASPNETCORE_URLS"];
var httpsPort = builder.Configuration.GetValue<int?>("ASPNETCORE_HTTPS_PORT");
var hasHttps = httpsPort.HasValue ||
               (!string.IsNullOrWhiteSpace(urls) &&
                urls.Contains("https://", StringComparison.OrdinalIgnoreCase));

if (hasHttps)
{
    app.UseHttpsRedirection();
}

app.UseCors("wasm");

app.UseAuthentication();
app.UseAuthorization();

var mediaOptions = app.Services.GetRequiredService<IOptions<MediaStorageOptions>>().Value;
var mediaRoot = Path.GetFullPath(mediaOptions.RootPath);
Directory.CreateDirectory(mediaRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(mediaRoot),
    RequestPath = "/media"
});

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
