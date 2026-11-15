using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using WLVSSeatSync.Application.Abstractions;

namespace WLVSSeatSync.Application.Common;

// Call after seats change and are SAVED: drops cached copies, then tells everyone watching.
public sealed class SeatMapUpdates(HybridCache cache, ISeatMapNotifier notifier, ILogger<SeatMapUpdates> logger)
{
    public async Task PublishAsync(Guid eventId, IReadOnlyCollection<Guid> eventSeatIds, bool isAvailable)
    {
        try
        {
            // CancellationToken.None: the change is saved, so finish even if the caller has gone.
            await cache.RemoveByTagAsync(CacheKeys.Event(eventId), CancellationToken.None);
            await notifier.SeatsChangedAsync(eventId, eventSeatIds, isAvailable, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // A failed notification must never turn a successful purchase into an error.
            logger.LogWarning(ex, "Could not publish seat changes for event {EventId}", eventId);
        }
    }
}
