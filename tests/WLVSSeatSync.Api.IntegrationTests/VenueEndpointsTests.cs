using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;

using Shouldly;

using WLVSSeatSync.Api.Endpoints;
using WLVSSeatSync.Application.Venues;

namespace WLVSSeatSync.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class VenueEndpointsTests(SeatSyncApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task An_admin_can_create_a_venue_and_see_its_sections()
    {
        await _client.LoginAsAsync("admin@seatsync.dev");

        var response = await _client.PostAsJsonAsync("/venues", new
        {
            name = "Test Hall",
            city = "Cebu City",
            sections = new[] { new { name = "Floor", rows = 3, seatsPerRow = 10 } },
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        var venue = await _client.GetFromJsonAsync<VenueDetails>($"/venues/{created!.Id}");
        venue!.Sections.ShouldHaveSingleItem().Seats.ShouldBe(30);
    }

    [Fact]
    public async Task Organizers_cannot_create_venues()
    {
        await _client.LoginAsAsync("organizer@seatsync.dev");

        var response = await _client.PostAsJsonAsync("/venues", new
        {
            name = "Test Hall",
            city = "Cebu City",
            sections = new[] { new { name = "Floor", rows = 3, seatsPerRow = 10 } },
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
