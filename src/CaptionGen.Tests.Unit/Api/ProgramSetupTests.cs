using CaptionGen.Api;
using CaptionGen.Application.Media;
using CaptionGen.Infrastructure.Captions;
using CaptionGen.Infrastructure.Media;
using FluentAssertions;
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

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();
}
