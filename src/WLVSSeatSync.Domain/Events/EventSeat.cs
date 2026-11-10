using WLVSSeatSync.Domain.Abstractions;

namespace WLVSSeatSync.Domain.Events;

public sealed class EventSeat : Entity
{
    private EventSeat() { } // for EF Core

    internal EventSeat(Guid eventId, Guid seatId, Guid ticketTierId)
    {
        EventId = eventId;
        SeatId = seatId;
        TicketTierId = ticketTierId;
        Status = SeatStatus.Available;
    }

    public Guid EventId { get; private set; }
    public Guid SeatId { get; private set; }        // the venue's Seat.Id
    public Guid TicketTierId { get; private set; }
    public SeatStatus Status { get; private set; }
    public Guid? OrderId { get; private set; }
    public DateTimeOffset? HeldUntil { get; private set; }
    public int Version { get; private set; }

    // An expired hold counts as available: rule 3.
    public bool IsAvailable(DateTimeOffset now) =>
        Status == SeatStatus.Available ||
        (Status == SeatStatus.Held && HeldUntil <= now);

    public bool IsHeldBy(Guid orderId, DateTimeOffset now) =>
        Status == SeatStatus.Held && OrderId == orderId && HeldUntil > now;

    internal void Hold(Guid orderId, DateTimeOffset heldUntil)
    {
        Status = SeatStatus.Held;
        OrderId = orderId;
        HeldUntil = heldUntil;
        Version++;
    }

    internal void MarkSold()
    {
        Status = SeatStatus.Sold;
        HeldUntil = null;
        Version++;
    }

    internal void Release()
    {
        Status = SeatStatus.Available;
        OrderId = null;
        HeldUntil = null;
        Version++;
    }
}
