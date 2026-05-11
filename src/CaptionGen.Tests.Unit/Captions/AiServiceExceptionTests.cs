using CaptionGen.Application.Captions;
using FluentAssertions;

namespace CaptionGen.Tests.Unit.Captions;

public sealed class AiServiceExceptionTests
{
    [Theory]
    [InlineData(400, true)]
    [InlineData(404, true)]
    [InlineData(500, false)]
    [InlineData(null, false)]
    public void IsClientError_ShouldReflectStatusCode(int? statusCode, bool expected)
    {
        var inner = new InvalidOperationException("inner");

        var sut = new AiServiceException("message", statusCode, inner);

        sut.Message.Should().Be("message");
        sut.StatusCode.Should().Be(statusCode);
        sut.InnerException.Should().BeSameAs(inner);
        sut.IsClientError.Should().Be(expected);
    }
}
