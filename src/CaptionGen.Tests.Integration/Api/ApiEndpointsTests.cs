using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
}

