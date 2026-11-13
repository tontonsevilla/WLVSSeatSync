using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc;

using Shouldly;

using WLVSSeatSync.Application.Events;
using WLVSSeatSync.Application.Orders;

namespace WLVSSeatSync.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class OrderEndpointsTests(SeatSyncApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task A_buyer_can_hold_pay_and_get_one_ticket_per_seat()
    {
        await _client.RegisterAndLoginAsync();
        var (eventId, seatIds) = await _client.FindAvailableSeatsAsync(2);

        var place = await _client.PostAsJsonAsync("/orders", new { eventId, eventSeatIds = seatIds });
        place.StatusCode.ShouldBe(HttpStatusCode.Created);
        var order = await place.Content.ReadFromJsonAsync<PlacedOrder>();

        var pay = await _client.PostAsJsonAsync($"/orders/{order!.OrderId}/pay", new { cardToken = "tok_visa" });
        pay.StatusCode.ShouldBe(HttpStatusCode.OK);
        var paid = await pay.Content.ReadFromJsonAsync<PaidOrder>();
        paid!.TicketCodes.Count.ShouldBe(2);

        var seatMap = await _client.GetFromJsonAsync<List<SeatMapSeat>>($"/events/{eventId}/seats");
        seatMap!.Where(s => seatIds.Contains(s.EventSeatId)).ShouldAllBe(s => !s.IsAvailable);
    }

    [Fact]
    public async Task Two_buyers_racing_for_one_seat_get_one_success_and_one_conflict()
    {
        var (eventId, seatIds) = await _client.FindAvailableSeatsAsync(1);

        var buyerA = factory.CreateClient();
        var buyerB = factory.CreateClient();
        await buyerA.RegisterAndLoginAsync();
        await buyerB.RegisterAndLoginAsync();

        // Fire both at once: they really do run in parallel inside the test server.
        var body = new { eventId, eventSeatIds = seatIds };
        var responses = await Task.WhenAll(
            buyerA.PostAsJsonAsync("/orders", body),
            buyerB.PostAsJsonAsync("/orders", body));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(1);
    }

    [Fact]
    public async Task A_declined_card_keeps_the_seats_held_so_the_buyer_can_retry()
    {
        await _client.RegisterAndLoginAsync();
        var (eventId, seatIds) = await _client.FindAvailableSeatsAsync(1);
        var place = await _client.PostAsJsonAsync("/orders", new { eventId, eventSeatIds = seatIds });
        var order = await place.Content.ReadFromJsonAsync<PlacedOrder>();

        var declined = await _client.PostAsJsonAsync($"/orders/{order!.OrderId}/pay", new { cardToken = "tok_declined" });
        declined.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await declined.Content.ReadFromJsonAsync<ProblemDetails>())!.Title.ShouldBe("Payment.Declined");

        var retry = await _client.PostAsJsonAsync($"/orders/{order.OrderId}/pay", new { cardToken = "tok_visa" });
        retry.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_promo_code_takes_20_percent_off()
    {
        await _client.RegisterAndLoginAsync();
        var (eventId, seatIds) = await _client.FindAvailableSeatsAsync(1);

        var details = await _client.GetFromJsonAsync<EventDetails>($"/events/{eventId}");
        var seatMap = await _client.GetFromJsonAsync<List<SeatMapSeat>>($"/events/{eventId}/seats");
        var tierId = seatMap!.Single(s => s.EventSeatId == seatIds[0]).TicketTierId;
        var basePrice = details!.TicketTiers.Single(t => t.Id == tierId).Price;

        var place = await _client.PostAsJsonAsync("/orders",
            new { eventId, eventSeatIds = seatIds, promoCode = "BIGGROUP20" });
        var placed = await place.Content.ReadFromJsonAsync<PlacedOrder>();

        placed!.AppliedDiscount.ShouldBe("Promo code");
        placed.Total.ShouldBe(decimal.Round(basePrice * 0.8m, 2));
    }

    [Fact]
    public async Task Someone_elses_order_does_not_exist_for_you()
    {
        await _client.RegisterAndLoginAsync();
        var (eventId, seatIds) = await _client.FindAvailableSeatsAsync(1);
        var place = await _client.PostAsJsonAsync("/orders", new { eventId, eventSeatIds = seatIds });
        var order = await place.Content.ReadFromJsonAsync<PlacedOrder>();

        var stranger = factory.CreateClient();
        await stranger.RegisterAndLoginAsync();

        (await stranger.GetAsync($"/orders/{order!.OrderId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await stranger.PostAsJsonAsync($"/orders/{order.OrderId}/pay", new { cardToken = "tok_visa" }))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ordering_requires_a_login()
    {
        var response = await _client.PostAsJsonAsync("/orders",
            new { eventId = Guid.NewGuid(), eventSeatIds = new[] { Guid.NewGuid() } });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cancelling_an_order_frees_its_seats_at_once()
    {
        await _client.RegisterAndLoginAsync();
        var (eventId, seatIds) = await _client.FindAvailableSeatsAsync(1);
        var place = await _client.PostAsJsonAsync("/orders", new { eventId, eventSeatIds = seatIds });
        var order = await place.Content.ReadFromJsonAsync<PlacedOrder>();

        var cancel = await _client.DeleteAsync($"/orders/{order!.OrderId}");
        cancel.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var seatMap = await _client.GetFromJsonAsync<List<SeatMapSeat>>($"/events/{eventId}/seats");
        seatMap!.Single(s => s.EventSeatId == seatIds[0]).IsAvailable.ShouldBeTrue();
    }
}
