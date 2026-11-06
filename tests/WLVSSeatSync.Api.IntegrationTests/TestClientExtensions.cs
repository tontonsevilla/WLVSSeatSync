using System.Net.Http.Headers;
using System.Net.Http.Json;

using WLVSSeatSync.Api.Auth;
using WLVSSeatSync.Infrastructure;

namespace WLVSSeatSync.Api.IntegrationTests;

internal static class TestClientExtensions
{
    public static async Task LoginAsAsync(this HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/auth/login",
            new { email, password = DatabaseSetup.DemoPassword });
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }
}
