using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;
using CaptionGen.Application.Auth;
using FluentAssertions;
using CaptionGen.Tests.Integration.Infra;

namespace CaptionGen.Tests.Integration.Api;

public sealed class ApiEndpointsTests : IClassFixture<CaptionGenApiFactory>
{
    private readonly CaptionGenApiFactory _factory;

    public ApiEndpointsTests(CaptionGenApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Auth_Register_Login_Me_ShouldWork()
    {
        using var client = _factory.CreateClient();

        var email = $"user{Guid.NewGuid():N}@example.com";
        var password = "P@ssw0rd!";

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginJson = await login.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        loginJson.Should().NotBeNull();
        loginJson!.TryGetValue("token", out var token).Should().BeTrue();
        token.Should().NotBeNullOrWhiteSpace();

        var me = await client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);

        var meBody = await me.Content.ReadFromJsonAsync<MeResponse>();
        meBody.Should().NotBeNull();
        meBody!.Email.Should().Be("integration@test.local");
        meBody.Id.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public async Task Posts_Create_ThenGet_ShouldReturnCreatedPost()
    {
        using var client = _factory.CreateClient();

        var email = $"user{Guid.NewGuid():N}@example.com";
        var password = "P@ssw0rd!";

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        var registerJson = await register.Content.ReadFromJsonAsync<JsonElement>();
        registerJson.TryGetProperty("id", out var userIdElement).Should().BeTrue();
        var userId = userIdElement.GetGuid();
        userId.Should().NotBeEmpty();

        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Email", email.ToLowerInvariant());

        var create = await client.PostAsJsonAsync("/api/posts", new
        {
            description = "hello",
            platform = "instagram",
            tone = "funny",
            count = 3
        });

        create.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        created.TryGetProperty("id", out var id).Should().BeTrue();
        id.GetGuid().Should().NotBeEmpty();

        var get = await client.GetAsync("/api/posts");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var posts = await get.Content.ReadFromJsonAsync<JsonElement>();
        posts.ValueKind.Should().Be(JsonValueKind.Array);
        posts.EnumerateArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task Media_Upload_List_Delete_ShouldWork()
    {
        using var client = _factory.CreateClient();

        var email = $"user{Guid.NewGuid():N}@example.com";
        var password = "P@ssw0rd!";

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        register.EnsureSuccessStatusCode();

        var registerJson = await register.Content.ReadFromJsonAsync<JsonElement>();
        var userId = registerJson.GetProperty("id").GetGuid();

        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Email", email.ToLowerInvariant());

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(fileContent, "file", "test.jpg");

        var upload = await client.PostAsync("/api/media/upload", form);
        upload.StatusCode.Should().Be(HttpStatusCode.OK);

        var uploaded = await upload.Content.ReadFromJsonAsync<JsonElement>();
        var mediaId = uploaded.GetProperty("id").GetGuid();
        mediaId.Should().NotBeEmpty();

        var list = await client.GetAsync("/api/media");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await list.Content.ReadFromJsonAsync<JsonElement>();
        items.EnumerateArray().Any(m => m.GetProperty("id").GetGuid() == mediaId).Should().BeTrue();

        var delete = await client.DeleteAsync($"/api/media/{mediaId}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listAfter = await client.GetAsync("/api/media");
        listAfter.StatusCode.Should().Be(HttpStatusCode.OK);
        var itemsAfter = await listAfter.Content.ReadFromJsonAsync<JsonElement>();
        itemsAfter.EnumerateArray().Any(m => m.GetProperty("id").GetGuid() == mediaId).Should().BeFalse();
    }

    [Fact]
    public async Task Posts_SelectCaption_AndSchedule_ShouldUpdateStatus()
    {
        using var client = _factory.CreateClient();

        var email = $"user{Guid.NewGuid():N}@example.com";
        var password = "P@ssw0rd!";

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        register.EnsureSuccessStatusCode();

        var registerJson = await register.Content.ReadFromJsonAsync<JsonElement>();
        var userId = registerJson.GetProperty("id").GetGuid();

        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Email", email.ToLowerInvariant());

        var create = await client.PostAsJsonAsync("/api/posts", new
        {
            description = "hello world long enough",
            platform = "instagram",
            tone = "funny",
            count = 2
        });
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var postId = created.GetProperty("id").GetGuid();

        var select = await client.PostAsync($"/api/posts/{postId}/select-caption/0", content: null);
        select.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var scheduleAt = DateTime.UtcNow.AddMinutes(10);
        var schedule = await client.PostAsJsonAsync($"/api/posts/{postId}/schedule", new
        {
            scheduledAtUtc = scheduleAt,
            selectedCaptionIndex = 0,
            mediaIds = Array.Empty<Guid>()
        });
        schedule.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await client.GetFromJsonAsync<JsonElement>("/api/posts");
        var scheduledPost = get.EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == postId);
        scheduledPost.GetProperty("status").GetString().Should().Be("scheduled");
        scheduledPost.TryGetProperty("scheduledAtUtc", out var scheduledAt).Should().BeTrue();
        scheduledAt.GetDateTime().Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task Captions_Improve_ShouldReturnVariants()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/captions/improve", new
        {
            caption = "test caption",
            platform = "instagram",
            tone = "funny",
            language = "en",
            goal = "engagement"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("improvedCaption", out var improved).Should().BeTrue();
        body.TryGetProperty("shorterVersion", out var shorter).Should().BeTrue();
        body.TryGetProperty("strongerCtaVersion", out var stronger).Should().BeTrue();
        improved.GetString().Should().NotBeNullOrWhiteSpace();
        shorter.GetString().Should().NotBeNullOrWhiteSpace();
        stronger.GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Payments_CreateCheckoutSession_ShouldReturnSession()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/payments/checkout-session", new
        {
            planSlug = "agency"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("sessionId", out var sessionId).Should().BeTrue();
        body.TryGetProperty("url", out var url).Should().BeTrue();
        sessionId.GetString().Should().NotBeNullOrWhiteSpace();
        url.GetString().Should().StartWith("https://checkout.stripe.com/");
    }
}
