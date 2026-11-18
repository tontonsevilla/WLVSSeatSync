using System.Net;
using System.Net.Http.Json;

using Shouldly;

using WLVSSeatSync.Api.Endpoints;
using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Application.Analytics;
using WLVSSeatSync.Application.Reviews;
using WLVSSeatSync.Application.Venues;

namespace WLVSSeatSync.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class DashboardEndpointsTests(SeatSyncApiFactory factory)
{
    private static readonly TimeZoneInfo Manila = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

    [Fact]
    public async Task Paid_seats_show_up_in_the_sales_report()
    {
        var buyer = factory.CreateClient();
        await buyer.RegisterAndLoginAsync();
        var (eventId, seatIds) = await buyer.FindAvailableSeatsAsync(2);
        await buyer.BuyAsync(eventId, seatIds);

        var organizer = factory.CreateClient();
        await organizer.LoginAsAsync("organizer@seatsync.dev");
        var report = await organizer.GetFromJsonAsync<EventSalesReport>($"/manage/events/{eventId}/sales");

        // Other tests may have bought seats for this event too, so "at least".
        report!.Tiers.Sum(t => t.SeatsSold).ShouldBeGreaterThanOrEqualTo(2);

        var today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Manila).Date;
        report.Days.ShouldContain(d => d.Day == DateOnly.FromDateTime(today) && d.Tickets >= 2);
    }

    [Fact]
    public async Task Only_ticket_holders_can_review_and_only_once()
    {
        var buyer = factory.CreateClient();
        await buyer.RegisterAndLoginAsync();
        var (eventId, seatIds) = await buyer.FindAvailableSeatsAsync(1);

        var tooEarly = await buyer.PostAsJsonAsync($"/events/{eventId}/reviews", new { rating = 4, comment = "Can't wait" });
        tooEarly.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity); // Review.NoTicket

        await buyer.BuyAsync(eventId, seatIds);

        var first = await buyer.PostAsJsonAsync($"/events/{eventId}/reviews", new { rating = 5, comment = "Great seats!" });
        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var second = await buyer.PostAsJsonAsync($"/events/{eventId}/reviews", new { rating = 1, comment = "Changed my mind" });
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict); // the unique index in MongoDB said no

        var reviews = await buyer.GetFromJsonAsync<EventReviews>($"/events/{eventId}/reviews");
        reviews!.Latest.ShouldContain(r => r.Rating == 5 && r.Comment == "Great seats!");
    }

    [Fact]
    public async Task Publishing_an_event_is_recorded_in_the_audit_log_for_admins()
    {
        var organizer = factory.CreateClient();
        await organizer.LoginAsAsync("organizer@seatsync.dev");
        var eventId = await CreateDraftEventAsync(organizer);

        var publish = await organizer.PostAsync($"/events/{eventId}/publish", content: null);
        publish.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var admin = factory.CreateClient();
        await admin.LoginAsAsync("admin@seatsync.dev");
        var entries = await admin.GetFromJsonAsync<List<AuditEntry>>("/admin/audit?action=PublishEvent&take=50");
        entries!.ShouldContain(e => e.UserEmail == "organizer@seatsync.dev" && e.Details.Contains(eventId.ToString()));

        var notAllowed = await organizer.GetAsync("/admin/audit");
        notAllowed.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<Guid> CreateDraftEventAsync(HttpClient organizer)
    {
        var venues = await organizer.GetFromJsonAsync<List<VenueSummary>>("/venues");
        var venue = await organizer.GetFromJsonAsync<VenueDetails>($"/venues/{venues![0].Id}");
        var startsAt = DateTimeOffset.UtcNow.AddDays(40);

        var response = await organizer.PostAsJsonAsync("/events", new
        {
            venueId = venue!.Id,
            title = "Audit test night",
            description = "",
            startsAt,
            endsAt = startsAt.AddHours(3),
            tiers = venue.Sections.Select(s => new { name = s.Name, section = s.Name, price = 1000m }).ToArray(),
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
    }
}
