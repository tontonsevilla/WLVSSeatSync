using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Common;

namespace WLVSSeatSync.Domain.Events;

public sealed class Event : AggregateRoot
{
    public static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(10);

    private readonly List<TicketTier> _ticketTiers = [];
    private readonly List<EventSeat> _seats = [];

    private Event() { } // for EF Core

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid VenueId { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public EventStatus Status { get; private set; }
    public IReadOnlyCollection<TicketTier> TicketTiers => _ticketTiers;
    public IReadOnlyCollection<EventSeat> Seats => _seats;

    public static Result<Event> Create(string title, string description, Guid venueId,
        DateTimeOffset startsAt, DateTimeOffset endsAt, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (startsAt <= now) return EventErrors.StartsInPast;
        if (endsAt <= startsAt) return EventErrors.EndsBeforeStart;

        return new Event
        {
            Title = title.Trim(),
            Description = description.Trim(),
            VenueId = venueId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Status = EventStatus.Draft,
        };
    }

    public Result<TicketTier> AddTicketTier(string name, Money price)
    {
        if (Status != EventStatus.Draft) return EventErrors.NotDraft;
        if (_ticketTiers.Any(t => t.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return EventErrors.DuplicateTier(name);

        var tier = new TicketTier(Id, name, price);
        _ticketTiers.Add(tier);
        return tier;
    }

    public Result AssignSeats(Guid ticketTierId, IReadOnlyCollection<Guid> venueSeatIds)
    {
        if (Status != EventStatus.Draft) return EventErrors.NotDraft;
        if (_ticketTiers.All(t => t.Id != ticketTierId)) return EventErrors.TierNotFound;

        var alreadyAssigned = _seats.Select(s => s.SeatId).ToHashSet();
        if (venueSeatIds.Distinct().Count() != venueSeatIds.Count || venueSeatIds.Any(alreadyAssigned.Contains))
            return EventErrors.SeatAlreadyAssigned;

        _seats.AddRange(venueSeatIds.Select(seatId => new EventSeat(Id, seatId, ticketTierId)));
        return Result.Success();
    }

    public Result Publish()
    {
        if (Status != EventStatus.Draft) return EventErrors.NotDraft;
        if (_seats.Count == 0) return EventErrors.NoSeats;

        Status = EventStatus.Published;
        Raise(new EventPublishedDomainEvent(Id));
        return Result.Success();
    }

    public Result HoldSeats(IReadOnlyCollection<Guid> eventSeatIds, Guid orderId, DateTimeOffset now)
    {
        if (Status != EventStatus.Published) return EventErrors.NotOnSale;
        if (now >= StartsAt) return EventErrors.AlreadyStarted;                 // rule 2
        if (eventSeatIds.Count == 0) return EventErrors.NoSeatsSelected;

        // Check every seat first and change nothing until all pass: all or nothing.
        var seats = new List<EventSeat>();
        foreach (var id in eventSeatIds.Distinct())
        {
            var seat = _seats.FirstOrDefault(s => s.Id == id);
            if (seat is null) return EventErrors.SeatNotFound(id);
            if (!seat.IsAvailable(now)) return EventErrors.SeatUnavailable(id); // rules 1 and 3
            seats.Add(seat);
        }

        var heldUntil = now + HoldDuration;
        foreach (var seat in seats) seat.Hold(orderId, heldUntil);
        return Result.Success();
    }

    public Result ConfirmSeats(IReadOnlyCollection<Guid> eventSeatIds, Guid orderId, DateTimeOffset now)
    {
        if (eventSeatIds.Count == 0) return EventErrors.NoSeatsSelected;

        var seats = new List<EventSeat>();
        foreach (var id in eventSeatIds.Distinct())
        {
            var seat = _seats.FirstOrDefault(s => s.Id == id);
            if (seat is null) return EventErrors.SeatNotFound(id);
            if (!seat.IsHeldBy(orderId, now)) return EventErrors.HoldExpired;    // rule 1
            seats.Add(seat);
        }

        foreach (var seat in seats) seat.MarkSold();
        return Result.Success();
    }

    // The Phase 3 background job calls this every minute.
    public int ReleaseExpiredHolds(DateTimeOffset now)
    {
        var expired = _seats.Where(s => s.Status == SeatStatus.Held && s.HeldUntil <= now).ToList();
        foreach (var seat in expired) seat.Release();
        return expired.Count;
    }
}
