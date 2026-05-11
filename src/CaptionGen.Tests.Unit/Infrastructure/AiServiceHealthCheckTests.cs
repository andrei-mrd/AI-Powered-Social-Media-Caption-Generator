using System.Net;
using CaptionGen.Infrastructure.Captions;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CaptionGen.Tests.Unit.Infrastructure;

public sealed class AiServiceHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenAiServiceReturnsOk_ShouldBeHealthy()
    {
        var sut = BuildSut(new HttpResponseMessage(HttpStatusCode.OK));

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("AI service reachable");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAiServiceReturnsError_ShouldBeDegraded()
    {
        var sut = BuildSut(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            ReasonPhrase = "Unavailable",
            Content = new StringContent("down")
        });

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("AI service responded with 503: Unavailable");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRequestThrows_ShouldBeUnhealthy()
    {
        var exception = new HttpRequestException("network");
        var sut = BuildSut(exception);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("AI service unreachable");
        result.Exception.Should().BeSameAs(exception);
    }

    private static AiServiceHealthCheck BuildSut(HttpResponseMessage response) =>
        new(
            new StubHttpClientFactory(new HttpClient(new StubHttpMessageHandler(_ => response))
            {
                BaseAddress = new Uri("https://ai.test/")
            }),
            Options.Create(new AiServiceOptions { HealthPath = "health" }),
            NullLogger<AiServiceHealthCheck>.Instance);

    private static AiServiceHealthCheck BuildSut(Exception exception) =>
        new(
            new StubHttpClientFactory(new HttpClient(new StubHttpMessageHandler(_ => throw exception))
            {
                BaseAddress = new Uri("https://ai.test/")
            }),
            Options.Create(new AiServiceOptions { HealthPath = "health" }),
            NullLogger<AiServiceHealthCheck>.Instance);

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StubHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_responseFactory(request));
    }
}
