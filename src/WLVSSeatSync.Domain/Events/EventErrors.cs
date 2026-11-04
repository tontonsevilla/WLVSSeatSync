using WLVSSeatSync.Domain.Abstractions;

namespace WLVSSeatSync.Domain.Events;

public static class EventErrors
{
    public static readonly Error StartsInPast = new("Event.StartsInPast", "An event must start in the future.");
    public static readonly Error EndsBeforeStart = new("Event.EndsBeforeStart", "An event must end after it starts.");
    public static readonly Error NotDraft = new("Event.NotDraft", "Only a draft event can be changed.");
    public static readonly Error TierNotFound = new("Event.TierNotFound", "The ticket tier does not belong to this event.");
    public static readonly Error SeatAlreadyAssigned = new("Event.SeatAlreadyAssigned", "A seat can be assigned to an event only once.");
    public static readonly Error NoSeats = new("Event.NoSeats", "Assign seats before publishing.");
    public static readonly Error NotOnSale = new("Event.NotOnSale", "Tickets for this event are not on sale.");
    public static readonly Error AlreadyStarted = new("Event.AlreadyStarted", "Tickets cannot be bought after the event starts.");
    public static readonly Error NoSeatsSelected = new("Event.NoSeatsSelected", "Select at least one seat.");
    public static readonly Error HoldExpired = new("Event.HoldExpired", "The seat hold has expired or belongs to another order.");

    public static Error DuplicateTier(string name) => new("Event.DuplicateTier", $"A tier named '{name}' already exists.");
    public static Error SeatNotFound(Guid id) => new("Event.SeatNotFound", $"Seat {id} is not part of this event.");
    public static Error SeatUnavailable(Guid id) => new("Event.SeatUnavailable", $"Seat {id} is already held or sold.");
}
