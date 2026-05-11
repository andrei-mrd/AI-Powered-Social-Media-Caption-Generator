using CaptionGen.Api.Contracts.Captions;
using CaptionGen.Api.Controllers;
using CaptionGen.Application.Captions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CaptionGen.Tests.Unit.Api;

public sealed class CaptionsControllerTests
{
    [Fact]
    public async Task Improve_WithValidRequest_ShouldReturnAiResult()
    {
        var request = new ImproveCaptionRequestDto
        {
            Caption = "caption",
            Platform = "Instagram",
            Tone = "Funny",
            Language = "en",
            Goal = "awareness"
        };
        var response = new CaptionImprovementResult("better", "short", "cta", "trace-1");
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(new ImproveCaptionCommand("caption", "Instagram", "Funny", "en", "awareness"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var sut = new CaptionsController(mediator.Object);

        var result = await sut.Improve(request, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task Improve_WhenValidationFails_ShouldReturnValidationProblem()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<ImproveCaptionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure("Caption", "Caption is required.")
            ]));
        var sut = new CaptionsController(mediator.Object);

        var result = await sut.Improve(
            new ImproveCaptionRequestDto
            {
                Caption = "",
                Platform = "instagram",
                Tone = "funny",
                Language = "en",
                Goal = "awareness"
            },
            CancellationToken.None);

        var problem = result.Should().BeAssignableTo<ObjectResult>().Subject.Value
            .Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Status.Should().Be(400);
        problem.Errors.Should().ContainKey("Caption");
    }

    [Theory]
    [InlineData(400)]
    [InlineData(502)]
    public async Task Improve_WhenAiFails_ShouldReturnMappedProblem(int expectedStatus)
    {
        var exception = expectedStatus == 400
            ? new AiServiceException("bad prompt", 400)
            : new AiServiceException("service down", 503);
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<ImproveCaptionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        var sut = new CaptionsController(mediator.Object);

        var result = await sut.Improve(
            new ImproveCaptionRequestDto
            {
                Caption = "caption",
                Platform = "instagram",
                Tone = "funny",
                Language = "en",
                Goal = "awareness"
            },
            CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(expectedStatus);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Title.Should().Be("Improve caption failed");
        problem.Detail.Should().Be(exception.Message);
    }
}
