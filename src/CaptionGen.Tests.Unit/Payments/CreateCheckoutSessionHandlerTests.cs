using CaptionGen.Application.Payments;
using FluentAssertions;
using Moq;

namespace CaptionGen.Tests.Unit.Payments;

public sealed class CreateCheckoutSessionHandlerTests
{
    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSession()
    {
        var payments = new Mock<IPaymentService>(MockBehavior.Strict);
        var userId = Guid.NewGuid();

        payments.Setup(x => x.CreateCheckoutSessionAsync(userId, "agency", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutSessionResult("cs_123", "https://stripe.test/cs_123"));

        var sut = new CreateCheckoutSessionHandler(payments.Object);

        var result = await sut.Handle(new CreateCheckoutSessionCommand(userId, " agency "), CancellationToken.None);

        result.SessionId.Should().Be("cs_123");
        result.Url.Should().Be("https://stripe.test/cs_123");

        payments.VerifyAll();
    }

    [Fact]
    public async Task Handle_WithEmptyPlan_ShouldThrow()
    {
        var payments = new Mock<IPaymentService>(MockBehavior.Loose);
        var sut = new CreateCheckoutSessionHandler(payments.Object);

        var act = () => sut.Handle(new CreateCheckoutSessionCommand(Guid.NewGuid(), " "), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
