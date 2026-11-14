using System.Collections.Concurrent;

using Microsoft.AspNetCore.SignalR;

namespace WLVSSeatSync.Api.Realtime;

// The messages the server can send to browsers. A typed interface means no magic strings in C#.
public interface ISeatMapClient
{
    Task SeatsChanged(Guid eventId, IReadOnlyCollection<Guid> eventSeatIds, bool isAvailable);
    Task ViewerCountChanged(Guid eventId, int viewers);
}

public sealed class SeatMapHub(ViewerCounter viewers) : Hub<ISeatMapClient>
{
    private const string EventIdKey = "eventId";

    public static string GroupFor(Guid eventId) => $"event-{eventId}";

    // The page calls this once connected: "send me updates for this event".
    public async Task WatchEvent(Guid eventId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(eventId));
        Context.Items[EventIdKey] = eventId;
        await Clients.Group(GroupFor(eventId)).ViewerCountChanged(eventId, viewers.Add(eventId));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue(EventIdKey, out var value) && value is Guid eventId)
            await Clients.Group(GroupFor(eventId)).ViewerCountChanged(eventId, viewers.Remove(eventId));

        await base.OnDisconnectedAsync(exception);
    }
}

// Counts viewers per event in this API instance's memory.
public sealed class ViewerCounter
{
    private readonly ConcurrentDictionary<Guid, int> _counts = new();

    public int Add(Guid eventId) => _counts.AddOrUpdate(eventId, 1, (_, n) => n + 1);
    public int Remove(Guid eventId) => _counts.AddOrUpdate(eventId, 0, (_, n) => Math.Max(0, n - 1));
}