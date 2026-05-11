using System.Text;
using CaptionGen.Api;
using CaptionGen.Application.Auth;
using CaptionGen.Application.Captions;
using CaptionGen.Application.Common.Policies;
using CaptionGen.Application.Common.Time;
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
using CaptionGen.Infrastructure.Common;
using CaptionGen.Infrastructure.Social;
using CaptionGen.Application.Social;
using JwtOptions = CaptionGen.Infrastructure.Auth.JwtOptions;
using CaptionGen.Application.Common.Validation;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Stripe;

ProgramSetup.LoadEnvFiles();

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
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
ProgramSetup.AddMediaStorage(builder.Services, builder.Configuration);
builder.Services.AddHostedService<ScheduledPostWorker>();
builder.Services.AddScoped<IEntitlementService, EntitlementService>();
builder.Services.AddScoped<IUsageService, UsageService>();
builder.Services.AddScoped<IPaymentService, StripePaymentService>();
builder.Services.AddScoped<IPaymentWebhookService, StripeWebhookService>();
builder.Services.AddSingleton<IContentPolicy, ContentPolicy>();
builder.Services.AddSingleton<ITimezoneService, TimezoneService>();

builder.Services.AddScoped<ISocialAccountRepository, SocialAccountRepository>();
builder.Services.AddSingleton<ITokenEncryptionService, AesTokenEncryptionService>();
builder.Services.AddHttpClient<ILinkedInOAuthService, LinkedInOAuthService>();
builder.Services.AddHttpClient<LinkedInPublisher>();
builder.Services.AddTransient<ISocialPublisher, LinkedInPublisher>();

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<AiServiceOptions>(
    builder.Configuration.GetSection(AiServiceOptions.SectionName));
builder.Services.Configure<MediaStorageOptions>(
    builder.Configuration.GetSection(MediaStorageOptions.SectionName));
builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.Configure<ContentPolicyOptions>(
    builder.Configuration.GetSection(ContentPolicyOptions.SectionName));
builder.Services.Configure<SchedulingOptions>(
    builder.Configuration.GetSection(SchedulingOptions.SectionName));
builder.Services.Configure<LinkedInOptions>(
    builder.Configuration.GetSection(LinkedInOptions.SectionName));
builder.Services.Configure<TokenEncryptionOptions>(
    builder.Configuration.GetSection(TokenEncryptionOptions.SectionName));

builder.Services.AddHttpClient("AiService.Health", ProgramSetup.ConfigureAiHttpClient);
builder.Services.AddHttpClient<IAiCaptionService, AiCaptionClient>(ProgramSetup.ConfigureAiHttpClient);

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

var jwtOpts = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

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
            ValidIssuer = jwtOpts.Issuer,
            ValidAudience = jwtOpts.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.Key)),
            ClockSkew = TimeSpan.FromSeconds(10)
        };

        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                if (ctx.Request.Cookies.TryGetValue(jwtOpts.CookieName, out var token))
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

ProgramSetup.UseGlobalExceptionHandler(app, allowedOrigins);

app.UseSwagger();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/swagger/v1/swagger.json", "CaptionGen API v1");
    opt.RoutePrefix = "docs";
});

ProgramSetup.UseHttpsRedirectionIfConfigured(app, builder.Configuration);

app.UseCors("wasm");

app.UseAuthentication();
app.UseAuthorization();

ProgramSetup.UseLocalMediaFiles(app);

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
