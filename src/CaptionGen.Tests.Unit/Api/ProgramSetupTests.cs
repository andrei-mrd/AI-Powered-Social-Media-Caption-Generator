using CaptionGen.Api;
using CaptionGen.Application.Media;
using CaptionGen.Infrastructure.Captions;
using CaptionGen.Infrastructure.Media;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CaptionGen.Tests.Unit.Api;

public sealed class ProgramSetupTests
{
    [Fact]
    public void AddMediaStorage_WithAzureBlobProvider_ShouldRegisterAzureImplementation()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(("MediaStorage:Provider", "AzureBlob"));

        ProgramSetup.AddMediaStorage(services, configuration);

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IMediaStorageService) &&
            descriptor.ImplementationType == typeof(AzureBlobMediaStorageService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddMediaStorage_WithDefaultProvider_ShouldRegisterLocalImplementation()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(("MediaStorage:Provider", "Local"));

        ProgramSetup.AddMediaStorage(services, configuration);

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IMediaStorageService) &&
            descriptor.ImplementationType == typeof(LocalMediaStorageService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Theory]
    [InlineData(2, 5)]
    [InlineData(45, 45)]
    [InlineData(500, 120)]
    public void ConfigureAiHttpClient_ShouldApplyBaseAddressAndClampTimeout(int configuredTimeout, int expectedTimeout)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new AiServiceOptions
        {
            BaseUrl = "https://ai.test/base/",
            TimeoutSeconds = configuredTimeout
        }));
        var provider = services.BuildServiceProvider();
        var client = new HttpClient();

        ProgramSetup.ConfigureAiHttpClient(provider, client);

        client.BaseAddress.Should().Be(new Uri("https://ai.test/base/"));
        client.Timeout.Should().Be(TimeSpan.FromSeconds(expectedTimeout));
    }

    [Fact]
    public void ConfigureAiHttpClient_WithInvalidBaseUrl_ShouldThrow()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new AiServiceOptions { BaseUrl = "not-a-url" }));
        var provider = services.BuildServiceProvider();
        var client = new HttpClient();

        var act = () => ProgramSetup.ConfigureAiHttpClient(provider, client);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("AiService:BaseUrl is not a valid absolute URI.");
    }

    [Fact]
    public void UseHttpsRedirectionIfConfigured_WhenHttpsUrlExists_ShouldNotThrow()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });
        var app = builder.Build();
        var configuration = BuildConfiguration(("ASPNETCORE_URLS", "http://localhost:5000;https://localhost:5001"));

        var act = () => ProgramSetup.UseHttpsRedirectionIfConfigured(app, configuration);

        act.Should().NotThrow();
    }

    [Fact]
    public void UseHttpsRedirectionIfConfigured_WhenHttpsPortExists_ShouldNotThrow()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });
        var app = builder.Build();
        var configuration = BuildConfiguration(("ASPNETCORE_HTTPS_PORT", "5001"));

        var act = () => ProgramSetup.UseHttpsRedirectionIfConfigured(app, configuration);

        act.Should().NotThrow();
    }

    [Fact]
    public void UseHttpsRedirectionIfConfigured_WhenHttpsIsNotConfigured_ShouldNotThrow()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });
        var app = builder.Build();
        var configuration = BuildConfiguration(("ASPNETCORE_URLS", "http://localhost:5000"));

        var act = () => ProgramSetup.UseHttpsRedirectionIfConfigured(app, configuration);

        act.Should().NotThrow();
    }

    [Fact]
    public void LoadEnvFiles_WhenEnvFileExists_ShouldSetValidEnvironmentVariables()
    {
        var envPath = GetApiAssemblyEnvPath();
        var previousFileExists = File.Exists(envPath);
        var previousContent = previousFileExists ? File.ReadAllText(envPath) : null;
        var previousValue = Environment.GetEnvironmentVariable("CAPTIONGEN_TEST_ENV");

        try
        {
            File.WriteAllLines(envPath,
            [
                "# comment",
                "",
                "CAPTIONGEN_TEST_ENV = loaded",
                "line-without-separator"
            ]);

            ProgramSetup.LoadEnvFiles();

            Environment.GetEnvironmentVariable("CAPTIONGEN_TEST_ENV").Should().Be("loaded");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CAPTIONGEN_TEST_ENV", previousValue);
            if (previousFileExists)
            {
                File.WriteAllText(envPath, previousContent);
            }
            else if (File.Exists(envPath))
            {
                File.Delete(envPath);
            }
        }
    }

    [Fact]
    public void LoadEnvFiles_WhenEnvFileDoesNotExist_ShouldReturnWithoutChanges()
    {
        var envPath = GetApiAssemblyEnvPath();
        var previousFileExists = File.Exists(envPath);
        var previousContent = previousFileExists ? File.ReadAllText(envPath) : null;

        try
        {
            if (File.Exists(envPath))
            {
                File.Delete(envPath);
            }

            var act = ProgramSetup.LoadEnvFiles;

            act.Should().NotThrow();
        }
        finally
        {
            if (previousFileExists)
            {
                File.WriteAllText(envPath, previousContent);
            }
        }
    }

    [Fact]
    public void UseLocalMediaFiles_WithAzureBlobProvider_ShouldSkipLocalDirectoryCreation()
    {
        var root = Path.Combine(Path.GetTempPath(), "captiongen-programsetup-tests", Guid.NewGuid().ToString("N"));
        var app = BuildMediaApp(new MediaStorageOptions
        {
            Provider = "AzureBlob",
            RootPath = root
        });

        ProgramSetup.UseLocalMediaFiles(app);

        Directory.Exists(root).Should().BeFalse();
    }

    [Fact]
    public void UseLocalMediaFiles_WithLocalProvider_ShouldCreateMediaDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "captiongen-programsetup-tests", Guid.NewGuid().ToString("N"));
        var app = BuildMediaApp(new MediaStorageOptions
        {
            Provider = "Local",
            RootPath = root
        });

        ProgramSetup.UseLocalMediaFiles(app);

        Directory.Exists(root).Should().BeTrue();
        Directory.Delete(root, recursive: true);
    }

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    private static WebApplication BuildMediaApp(MediaStorageOptions options)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });
        builder.Services.AddSingleton(Options.Create(options));
        return builder.Build();
    }

    private static string GetApiAssemblyEnvPath()
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(ProgramSetup).Assembly.Location)!;
        return Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", "..", ".env"));
    }
}
