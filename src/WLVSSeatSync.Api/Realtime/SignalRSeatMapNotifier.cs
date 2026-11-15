using Microsoft.AspNetCore.SignalR;

using WLVSSeatSync.Application.Abstractions;

namespace WLVSSeatSync.Api.Realtime;

// Lets code outside the hub (our command handlers) send messages to connected browsers.
internal sealed class SignalRSeatMapNotifier(IHubContext<SeatMapHub, ISeatMapClient> hub) : ISeatMapNotifier
{
    public Task SeatsChangedAsync(Guid eventId, IReadOnlyCollection<Guid> eventSeatIds, bool isAvailable,
        CancellationToken cancellationToken) =>
        hub.Clients.Group(SeatMapHub.GroupFor(eventId)).SeatsChanged(eventId, eventSeatIds, isAvailable);
}