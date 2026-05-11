using System.Net;
using System.Text;
using CaptionGen.Application.Captions;
using CaptionGen.Infrastructure.Captions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CaptionGen.Tests.Unit.Infrastructure;

public sealed class AiCaptionClientTests
{
    [Fact]
    public async Task GenerateAsync_WithSuccessfulResponse_ShouldReturnCaptionsAndBestHashtags()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "captions": [
                    { "text": "first", "hashtags": [], "hook": "", "cta": "go", "score": 80, "score_reason": "ok" },
                    { "text": "second", "hashtags": ["#b"], "hook": "hook", "cta": "", "score": 90, "score_reason": "better" }
                  ],
                  "best_caption_index": 1,
                  "metadata": { "engagement_score": 91, "engagement_rationale": "strong", "trace_id": "meta-trace" }
                }
                """,
                Encoding.UTF8,
                "application/json")
        });
        var sut = BuildClient(handler);

        var result = await sut.GenerateAsync(
            "launch",
            "instagram",
            "funny",
            2,
            new CaptionGenerationOptions(
                "en",
                "awareness",
                "short",
                true,
                true,
                5,
                "fans",
                "warm",
                ["boring"],
                ["launch"],
                ["https://cdn.test/image.png"]),
            CancellationToken.None);

        result.Captions.Select(c => c.Text).Should().ContainInOrder("first", "second");
        result.Hashtags.Should().Equal("#b");
        result.EngagementScore.Should().Be(91);
        result.EngagementRationale.Should().Be("strong");
        result.TraceId.Should().Be("meta-trace");
        handler.LastRequest!.RequestUri!.ToString().Should().Be("https://ai.test/generate-caption");
        handler.LastRequest.Headers.GetValues("X-Request-ID").Should().ContainSingle();
    }

    [Fact]
    public async Task GenerateAsync_WithErrorResponse_ShouldThrowAiServiceExceptionWithDetail()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"detail":"unsafe input"}""", Encoding.UTF8, "application/json")
        });
        var sut = BuildClient(handler);

        var act = () => sut.GenerateAsync(
            "bad",
            "instagram",
            "funny",
            1,
            new CaptionGenerationOptions("en", "awareness", "short", true, true, 3, null, null, [], [], []),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AiServiceException>();
        ex.Which.Message.Should().Be("unsafe input");
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.IsClientError.Should().BeTrue();
    }

    [Fact]
    public async Task ImproveAsync_WithSuccessfulResponse_ShouldReturnImprovement()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "improved_caption": "better",
                  "shorter_version": "short",
                  "stronger_cta_version": "buy",
                  "trace_id": "trace-123"
                }
                """,
                Encoding.UTF8,
                "application/json")
        });
        var sut = BuildClient(handler);

        var result = await sut.ImproveAsync("caption", "linkedin", "professional", "en", "sales", CancellationToken.None);

        result.ImprovedCaption.Should().Be("better");
        result.ShorterVersion.Should().Be("short");
        result.StrongerCtaVersion.Should().Be("buy");
        result.TraceId.Should().Be("trace-123");
        handler.LastRequest!.RequestUri!.ToString().Should().Be("https://ai.test/improve-caption");
    }

    [Fact]
    public async Task ImproveAsync_WithEmptyPayload_ShouldThrowAiServiceException()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"improved_caption":""}""", Encoding.UTF8, "application/json")
        });
        var sut = BuildClient(handler);

        var act = () => sut.ImproveAsync("caption", "linkedin", "professional", "en", "sales", CancellationToken.None);

        await act.Should().ThrowAsync<AiServiceException>()
            .WithMessage("AI service returned an empty improved caption.");
    }

    private static AiCaptionClient BuildClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://ai.test/")
        };

        return new AiCaptionClient(httpClient, NullLogger<AiCaptionClient>.Instance);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responseFactory(request));
        }
    }
}
