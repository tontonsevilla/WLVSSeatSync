namespace WLVSSeatSync.Application.Abstractions;

public interface ISeatMapNotifier
{
    Task SeatsChangedAsync(Guid eventId, IReadOnlyCollection<Guid> eventSeatIds, bool isAvailable,
        CancellationToken cancellationToken);
}
