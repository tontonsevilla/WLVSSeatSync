using System.Net.Http.Headers;
using System.Net.Http.Json;

using WLVSSeatSync.Api.Auth;
using WLVSSeatSync.Application.Common;
using WLVSSeatSync.Application.Events;
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

    public static async Task RegisterAndLoginAsync(this HttpClient client)
    {
        var email = $"buyer-{Guid.NewGuid():N}@example.com";
        const string password = "Str0ng!Passw0rd";

        var register = await client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Test Buyer" });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var token = await login.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }

    // Earlier tests buy seats too, so always look for seats that are free right now.
    public static async Task<(Guid EventId, List<Guid> SeatIds)> FindAvailableSeatsAsync(this HttpClient client, int count)
    {
        var page = await client.GetFromJsonAsync<PagedList<EventSummary>>("/events?pageSize=50");

        foreach (var ev in page!.Items)
        {
            var seats = await client.GetFromJsonAsync<List<SeatMapSeat>>($"/events/{ev.Id}/seats");
            var free = seats!.Where(s => s.IsAvailable).Take(count).Select(s => s.EventSeatId).ToList();
            if (free.Count == count)
                return (ev.Id, free);
        }

        throw new InvalidOperationException($"No event has {count} free seats.");
    }
}
