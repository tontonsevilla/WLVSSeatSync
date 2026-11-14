using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

using Shouldly;

namespace WLVSSeatSync.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class SeatMapHubTests(SeatSyncApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task People_watching_an_event_are_told_the_moment_its_seats_are_held()
    {
        var (eventId, seatIds) = await _client.FindAvailableSeatsAsync(1);

        await using var watcher = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/seat-map"), options =>
            {
                // Route the connection through the in-memory test server instead of the network.
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        var seatsChanged = new TaskCompletionSource<(Guid EventId, Guid[] SeatIds, bool IsAvailable)>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        watcher.On<Guid, Guid[], bool>("SeatsChanged", (changedEventId, changedSeatIds, isAvailable) =>
            seatsChanged.TrySetResult((changedEventId, changedSeatIds, isAvailable)));

        await watcher.StartAsync();
        await watcher.InvokeAsync("WatchEvent", eventId);

        // Someone else holds the seat...
        await _client.RegisterAndLoginAsync();
        var place = await _client.PostAsJsonAsync("/orders", new { eventId, eventSeatIds = seatIds });
        place.StatusCode.ShouldBe(HttpStatusCode.Created);

        // ...and the watcher hears about it.
        var message = await seatsChanged.Task.WaitAsync(TimeSpan.FromSeconds(10));
        message.EventId.ShouldBe(eventId);
        message.SeatIds.ShouldContain(seatIds[0]);
        message.IsAvailable.ShouldBeFalse();
    }
}