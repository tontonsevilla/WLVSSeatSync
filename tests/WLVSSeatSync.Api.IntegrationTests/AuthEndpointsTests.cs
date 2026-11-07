using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Shouldly;

using WLVSSeatSync.Api.Auth;
using WLVSSeatSync.Api.Endpoints;
using WLVSSeatSync.Application.Abstractions;

namespace WLVSSeatSync.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AuthEndpointsTests(SeatSyncApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task A_new_user_can_register_and_log_in_as_an_attendee()
    {
        var email = $"test-{Guid.NewGuid():N}@example.com";
        const string password = "Str0ng!Passw0rd";

        var register = await _client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Test User" });
        register.StatusCode.ShouldBe(HttpStatusCode.OK);

        var login = await _client.PostAsJsonAsync("/auth/login", new { email, password });
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        var token = await login.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        var me = await _client.GetFromJsonAsync<MeResponse>("/auth/me");

        me.ShouldNotBeNull();
        me.Email.ShouldBe(email);
        me.Roles.ShouldHaveSingleItem().ShouldBe(Roles.Attendee);
    }

    [Fact]
    public async Task A_wrong_password_returns_401()
    {
        var response = await _client.PostAsJsonAsync("/auth/login",
            new { email = "organizer@seatsync.dev", password = "wrong-password" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
