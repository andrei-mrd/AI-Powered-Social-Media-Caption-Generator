using System.Security.Claims;
using System.Text;
using CaptionGen.Api.Contracts.Payments;
using CaptionGen.Api.Controllers;
using CaptionGen.Application.Payments;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CaptionGen.Tests.Unit.Api;

public sealed class PaymentsControllerTests
{
    [Fact]
    public async Task CreateCheckoutSession_WithoutUserId_ShouldReturnUnauthorized()
    {
        var sut = BuildController(new Mock<IMediator>(MockBehavior.Strict), new Mock<IPaymentWebhookService>(MockBehavior.Strict));

        var result = await sut.CreateCheckoutSession(new CreateCheckoutSessionRequest { PlanSlug = "basic" }, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CreateCheckoutSession_WithUserId_ShouldReturnSession()
    {
        var userId = Guid.NewGuid();
        var session = new CheckoutSessionResult("cs_123", "https://checkout.test/cs_123");
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(new CreateCheckoutSessionCommand(userId, "agency"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var sut = BuildController(mediator, new Mock<IPaymentWebhookService>(MockBehavior.Strict), userId);

        var result = await sut.CreateCheckoutSession(new CreateCheckoutSessionRequest { PlanSlug = "agency" }, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(session);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task CreateCheckoutSession_WhenValidationFails_ShouldReturnValidationProblem()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<CreateCheckoutSessionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure("PlanSlug", "Plan is required.")
            ]));
        var sut = BuildController(mediator, new Mock<IPaymentWebhookService>(MockBehavior.Strict), Guid.NewGuid());

        var result = await sut.CreateCheckoutSession(new CreateCheckoutSessionRequest { PlanSlug = "" }, CancellationToken.None);

        var problem = result.Should().BeAssignableTo<ObjectResult>().Subject.Value
            .Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Status.Should().Be(400);
        problem.Errors.Should().ContainKey("PlanSlug");
    }

    [Fact]
    public async Task Webhook_WithoutSignature_ShouldReturnBadRequest()
    {
        var sut = BuildController(new Mock<IMediator>(MockBehavior.Strict), new Mock<IPaymentWebhookService>(MockBehavior.Strict));

        var result = await sut.Webhook(null, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().Be("Missing Stripe-Signature header.");
    }

    [Fact]
    public async Task Webhook_WithSignature_ShouldForwardPayload()
    {
        var webhook = new Mock<IPaymentWebhookService>(MockBehavior.Strict);
        webhook.Setup(x => x.HandleAsync("""{"id":"evt_1"}""", "sig", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = BuildController(new Mock<IMediator>(MockBehavior.Strict), webhook);
        sut.ControllerContext.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""{"id":"evt_1"}"""));

        var result = await sut.Webhook("sig", CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        webhook.VerifyAll();
    }

    [Theory]
    [InlineData(true, 400)]
    [InlineData(false, 500)]
    public async Task Webhook_WhenPaymentServiceRejects_ShouldReturnProblem(bool isClientError, int expectedStatus)
    {
        var webhook = new Mock<IPaymentWebhookService>(MockBehavior.Strict);
        webhook.Setup(x => x.HandleAsync(It.IsAny<string>(), "sig", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentServiceException("rejected", isClientError));
        var sut = BuildController(new Mock<IMediator>(MockBehavior.Strict), webhook);
        sut.ControllerContext.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

        var result = await sut.Webhook("sig", CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(expectedStatus);
        objectResult.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Be("rejected");
    }

    private static PaymentsController BuildController(
        Mock<IMediator> mediator,
        Mock<IPaymentWebhookService> webhookService,
        Guid? userId = null)
    {
        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())],
                "test"));
        }

        return new PaymentsController(mediator.Object, webhookService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
