using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CaptionGen.Application.Entitlements;
using CaptionGen.Application.Payments;
using CaptionGen.Infrastructure.Payments;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace CaptionGen.Tests.Unit.Payments;

public sealed class StripeWebhookServiceTests
{
    [Fact]
    public async Task HandleAsync_WithValidSignature_ShouldAssignPlan()
    {
        var secret = "whsec_test";
        var userId = Guid.NewGuid();

        var payloadObj = new
        {
            id = "evt_1",
            type = "checkout.session.completed",
            data = new
            {
                @object = new
                {
                    id = "cs_test",
                    @object = "checkout.session",
                    metadata = new Dictionary<string, string>
                    {
                        { "userId", userId.ToString() },
                        { "plan", "agency" }
                    }
                }
            }
        };

        var payload = JsonSerializer.Serialize(payloadObj);
        var sigHeader = BuildSignatureHeader(payload, secret);

        var entitlements = new Mock<IEntitlementService>(MockBehavior.Strict);
        entitlements.Setup(x => x.AssignPlanAsync(userId, "agency", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = Options.Create(new StripeOptions { WebhookSecret = secret });
        var sut = new StripeWebhookService(options, entitlements.Object, NullLogger<StripeWebhookService>.Instance);

        await sut.HandleAsync(payload, sigHeader, CancellationToken.None);

        entitlements.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WithInvalidSignature_ShouldThrow()
    {
        var entitlements = new Mock<IEntitlementService>(MockBehavior.Loose);
        var options = Options.Create(new StripeOptions { WebhookSecret = "whsec_test" });
        var sut = new StripeWebhookService(options, entitlements.Object, NullLogger<StripeWebhookService>.Instance);

        var act = () => sut.HandleAsync("{}", "t=0,v1=bad", CancellationToken.None);

        await act.Should().ThrowAsync<PaymentServiceException>();
    }

    [Fact]
    public async Task HandleAsync_WithSubscriptionUpdated_ShouldAssignPlanFromPriceId()
    {
        var secret = "whsec_test";
        var userId = Guid.NewGuid();

        var payloadObj = new
        {
            id = "evt_2",
            type = "customer.subscription.updated",
            data = new
            {
                @object = new
                {
                    id = "sub_test",
                    @object = "subscription",
                    metadata = new Dictionary<string, string>
                    {
                        { "userId", userId.ToString() }
                    },
                    items = new
                    {
                        @object = "list",
                        data = new object[]
                        {
                            new
                            {
                                id = "si_test",
                                @object = "subscription_item",
                                price = new
                                {
                                    id = "price_test_agency",
                                    @object = "price"
                                }
                            }
                        }
                    }
                }
            }
        };

        var payload = JsonSerializer.Serialize(payloadObj);
        var sigHeader = BuildSignatureHeader(payload, secret);

        var entitlements = new Mock<IEntitlementService>(MockBehavior.Strict);
        entitlements.Setup(x => x.AssignPlanAsync(userId, "agency", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = Options.Create(new StripeOptions
        {
            WebhookSecret = secret,
            PriceIds = new Dictionary<string, string>
            {
                ["agency"] = "price_test_agency"
            }
        });

        var sut = new StripeWebhookService(options, entitlements.Object, NullLogger<StripeWebhookService>.Instance);

        await sut.HandleAsync(payload, sigHeader, CancellationToken.None);

        entitlements.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WithSubscriptionDeleted_ShouldDowngradeToBasic()
    {
        var secret = "whsec_test";
        var userId = Guid.NewGuid();

        var payloadObj = new
        {
            id = "evt_3",
            type = "customer.subscription.deleted",
            data = new
            {
                @object = new
                {
                    id = "sub_test",
                    @object = "subscription",
                    metadata = new Dictionary<string, string>
                    {
                        { "userId", userId.ToString() }
                    }
                }
            }
        };

        var payload = JsonSerializer.Serialize(payloadObj);
        var sigHeader = BuildSignatureHeader(payload, secret);

        var entitlements = new Mock<IEntitlementService>(MockBehavior.Strict);
        entitlements.Setup(x => x.AssignPlanAsync(userId, "basic", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = Options.Create(new StripeOptions { WebhookSecret = secret });
        var sut = new StripeWebhookService(options, entitlements.Object, NullLogger<StripeWebhookService>.Instance);

        await sut.HandleAsync(payload, sigHeader, CancellationToken.None);

        entitlements.VerifyAll();
    }

    private static string BuildSignatureHeader(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var signature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }
}
