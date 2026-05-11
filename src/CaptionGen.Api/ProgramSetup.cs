using CaptionGen.Application.Media;
using CaptionGen.Infrastructure.Captions;
using CaptionGen.Infrastructure.Media;
using CaptionGen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace CaptionGen.Api;

public static class ProgramSetup
{
    private const string AzureBlobProvider = "AzureBlob";

    public static void LoadEnvFiles()
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(global::Program).Assembly.Location);
        if (string.IsNullOrWhiteSpace(assemblyDirectory))
        {
            return;
        }

        var path = Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", "..", ".env"));
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var line in File.ReadAllLines(path))
        {
            SetEnvironmentVariable(line);
        }
    }

    public static void AddMediaStorage(IServiceCollection services, IConfiguration configuration)
    {
        var implementationType = IsAzureBlobProvider(configuration["MediaStorage:Provider"])
            ? typeof(AzureBlobMediaStorageService)
            : typeof(LocalMediaStorageService);

        services.AddScoped(typeof(IMediaStorageService), implementationType);
    }

    public static void ConfigureAiHttpClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var options = serviceProvider.GetRequiredService<IOptions<AiServiceOptions>>().Value;
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("AiService:BaseUrl is not a valid absolute URI.");
        }

        client.BaseAddress = uri;
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 120));
    }

    public static void UseGlobalExceptionHandler(WebApplication app, string[] allowedOrigins)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                LogUnhandledException(context);
                AddCorsHeadersForKnownOrigin(context, allowedOrigins);

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
    }

    public static void UseHttpsRedirectionIfConfigured(WebApplication app, IConfiguration configuration)
    {
        var urls = configuration["ASPNETCORE_URLS"];
        var httpsPort = configuration.GetValue<int?>("ASPNETCORE_HTTPS_PORT");
        var hasHttps = httpsPort.HasValue ||
                       (!string.IsNullOrWhiteSpace(urls) &&
                        urls.Contains("https://", StringComparison.OrdinalIgnoreCase));

        if (hasHttps)
        {
            app.UseHttpsRedirection();
        }
    }

    public static void UseLocalMediaFiles(WebApplication app)
    {
        var mediaOptions = app.Services.GetRequiredService<IOptions<MediaStorageOptions>>().Value;
        if (IsAzureBlobProvider(mediaOptions.Provider))
        {
            return;
        }

        var mediaRoot = Path.GetFullPath(mediaOptions.RootPath);
        Directory.CreateDirectory(mediaRoot);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(mediaRoot),
            RequestPath = "/media"
        });
    }

    private static void SetEnvironmentVariable(string line)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
        {
            return;
        }

        var separatorIndex = trimmed.IndexOf('=', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return;
        }

        var key = trimmed[..separatorIndex].Trim();
        var value = trimmed[(separatorIndex + 1)..].Trim();
        if (!string.IsNullOrWhiteSpace(key))
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static bool IsAzureBlobProvider(string? provider)
        => string.Equals(provider, AzureBlobProvider, StringComparison.OrdinalIgnoreCase);

    private static void LogUnhandledException(HttpContext context)
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error is null)
        {
            return;
        }

        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalException");
        logger.LogError(feature.Error, "Unhandled exception");
    }

    private static void AddCorsHeadersForKnownOrigin(HttpContext context, string[] allowedOrigins)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(origin) ||
            !allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        context.Response.Headers.AccessControlAllowOrigin = origin;
        context.Response.Headers.AccessControlAllowCredentials = "true";
    }
}
