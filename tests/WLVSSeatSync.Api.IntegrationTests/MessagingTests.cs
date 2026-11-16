using System.Net;
using System.Net.Http.Json;

using MassTransit.Testing;

using Shouldly;

using WLVSSeatSync.Application.Orders;
using WLVSSeatSync.Contracts;

namespace WLVSSeatSync.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class MessagingTests(SeatSyncApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Paying_publishes_OrderPaid_with_one_ticket_per_seat()
    {
        var harness = factory.Services.GetTestHarness();

        await _client.RegisterAndLoginAsync();
        var (eventId, seatIds) = await _client.FindAvailableSeatsAsync(2);
        var place = await _client.PostAsJsonAsync("/orders", new { eventId, eventSeatIds = seatIds });
        var order = await place.Content.ReadFromJsonAsync<PlacedOrder>();

        var pay = await _client.PostAsJsonAsync($"/orders/{order!.OrderId}/pay", new { cardToken = "tok_visa" });
        pay.StatusCode.ShouldBe(HttpStatusCode.OK);

        // The outbox delivers in the background, so Any() waits for the message to show up.
        (await harness.Published.Any<OrderPaid>(m => m.Context.Message.OrderId == order.OrderId)).ShouldBeTrue();

        var message = harness.Published.Select<OrderPaid>()
            .Single(m => m.Context.Message.OrderId == order.OrderId)
            .Context.Message;

        message.Tickets.Count.ShouldBe(2);
        message.Tickets.ShouldAllBe(t => t.Code.Length == 32);
        message.CustomerEmail.ShouldEndWith("@example.com");
    }

    [Fact]
    public async Task A_declined_payment_publishes_nothing()
    {
        var harness = factory.Services.GetTestHarness();

        await _client.RegisterAndLoginAsync();
        var (eventId, seatIds) = await _client.FindAvailableSeatsAsync(1);
        var place = await _client.PostAsJsonAsync("/orders", new { eventId, eventSeatIds = seatIds });
        var order = await place.Content.ReadFromJsonAsync<PlacedOrder>();

        var pay = await _client.PostAsJsonAsync($"/orders/{order!.OrderId}/pay", new { cardToken = "tok_declined" });
        pay.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        (await harness.Published.Any<OrderPaid>(m => m.Context.Message.OrderId == order.OrderId)).ShouldBeFalse();
    }
}
