using CaptionGen.Application.Common.Policies;
using CaptionGen.Infrastructure.Common;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CaptionGen.Tests.Unit.Infrastructure;

public sealed class ContentPolicyTests
{
    private static ContentPolicy BuildSut() =>
        new(Options.Create(new ContentPolicyOptions
        {
            Platforms = new() { "instagram", "tiktok", "linkedin" },
            Tones = new() { "funny", "professional", "inspirational" },
            Goals = new() { "engagement", "sales", "awareness" },
            CaptionLengths = new() { "short", "medium", "long" },
            MinCaptionCount = 1,
            MaxCaptionCount = 10,
            MinHashtags = 5,
            MaxHashtags = 20
        }));

    [Theory]
    [InlineData("instagram", true)]
    [InlineData("INSTAGRAM", true)]
    [InlineData("  tiktok  ", true)]
    [InlineData("facebook", false)]
    [InlineData("", false)]
    public void IsSupportedPlatform_ShouldMatchCaseInsensitive(string value, bool expected)
    {
        BuildSut().IsSupportedPlatform(value).Should().Be(expected);
    }

    [Theory]
    [InlineData("funny", true)]
    [InlineData("PROFESSIONAL", true)]
    [InlineData("sarcastic", false)]
    public void IsSupportedTone_ShouldMatchCaseInsensitive(string value, bool expected)
    {
        BuildSut().IsSupportedTone(value).Should().Be(expected);
    }

    [Theory]
    [InlineData("engagement", true)]
    [InlineData("SALES", true)]
    [InlineData("conversion", false)]
    public void IsSupportedGoal_ShouldMatchCaseInsensitive(string value, bool expected)
    {
        BuildSut().IsSupportedGoal(value).Should().Be(expected);
    }

    [Theory]
    [InlineData("short", true)]
    [InlineData("MEDIUM", true)]
    [InlineData("extra-long", false)]
    public void IsSupportedCaptionLength_ShouldMatchCaseInsensitive(string value, bool expected)
    {
        BuildSut().IsSupportedCaptionLength(value).Should().Be(expected);
    }

    [Fact]
    public void AllowedPlatforms_ShouldReturnConfiguredValues()
    {
        BuildSut().AllowedPlatforms.Should().BeEquivalentTo("instagram", "tiktok", "linkedin");
    }

    [Fact]
    public void Limits_ShouldMatchConfiguration()
    {
        var sut = BuildSut();
        sut.MinCaptionCount.Should().Be(1);
        sut.MaxCaptionCount.Should().Be(10);
        sut.MinHashtags.Should().Be(5);
        sut.MaxHashtags.Should().Be(20);
    }
}
