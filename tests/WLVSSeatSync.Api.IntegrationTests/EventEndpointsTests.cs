using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Shouldly;

using WLVSSeatSync.Api.Endpoints;
using WLVSSeatSync.Application.Common;
using WLVSSeatSync.Application.Events;
using WLVSSeatSync.Application.Venues;

namespace WLVSSeatSync.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class EventEndpointsTests(SeatSyncApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Anyone_can_search_upcoming_events_in_date_order()
    {
        var page = await _client.GetFromJsonAsync<PagedList<EventSummary>>("/events?pageSize=5");

        page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(5);
        page.TotalCount.ShouldBeGreaterThanOrEqualTo(50);
        page.Items.Select(e => e.StartsAt).ShouldBeInOrder();
    }

    [Fact]
    public async Task An_unknown_event_returns_404_problem_details()
    {
        var response = await _client.GetAsync($"/events/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Event.NotFound");
    }

    [Fact]
    public async Task The_seat_map_shows_every_seat_of_a_new_event_as_available()
    {
        var page = await _client.GetFromJsonAsync<PagedList<EventSummary>>("/events?pageSize=1");

        var seats = await _client.GetFromJsonAsync<List<SeatMapSeat>>($"/events/{page!.Items[0].Id}/seats");

        seats.ShouldNotBeNull();
        seats.ShouldNotBeEmpty();
        seats.ShouldAllBe(s => s.IsAvailable);
    }

    [Fact]
    public async Task Creating_an_event_requires_a_login()
    {
        var response = await _client.PostAsJsonAsync("/events", ValidEvent(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Attendees_cannot_create_events()
    {
        await _client.LoginAsAsync("attendee@seatsync.dev");

        var response = await _client.PostAsJsonAsync("/events", ValidEvent(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Invalid_events_are_rejected_with_field_errors()
    {
        await _client.LoginAsAsync("organizer@seatsync.dev");
        var invalid = ValidEvent(Guid.NewGuid()) with { Title = "", Tiers = [] };

        var response = await _client.PostAsJsonAsync("/events", invalid);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors.Keys.ShouldContain("Title");
        problem.Errors.Keys.ShouldContain("Tiers");
    }

    [Fact]
    public async Task An_organizer_can_create_and_publish_an_event()
    {
        await _client.LoginAsAsync("organizer@seatsync.dev");
        var venues = await _client.GetFromJsonAsync<List<VenueSummary>>("/venues");

        var create = await _client.PostAsJsonAsync("/events", ValidEvent(venues![0].Id));
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<CreatedResponse>();

        var publish = await _client.PostAsync($"/events/{created!.Id}/publish", content: null);
        publish.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var details = await _client.GetFromJsonAsync<EventDetails>($"/events/{created.Id}");
        details!.Status.ShouldBe("Published");
        details.TicketTiers.Count.ShouldBe(2);
        details.TicketTiers.ShouldAllBe(t => t.AvailableSeats == t.TotalSeats);
    }

    [Fact]
    public async Task Every_response_echoes_a_safe_correlation_id()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/venues");
        request.Headers.Add("X-Correlation-ID", "test-123");

        var response = await _client.SendAsync(request);

        response.Headers.GetValues("X-Correlation-ID").ShouldHaveSingleItem().ShouldBe("test-123");
    }

    private static CreateEventCommand ValidEvent(Guid venueId)
    {
        var startsAt = DateTimeOffset.UtcNow.AddDays(60);
        return new CreateEventCommand(venueId, "Integration Test Concert", "Created by a test.",
            startsAt, startsAt.AddHours(3),
            [new CreateTicketTier("VIP", "VIP", 4_500m), new CreateTicketTier("General", "Upper Box", 1_200m)]);
    }
}
