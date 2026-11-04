using WLVSSeatSync.Domain.Abstractions;

namespace WLVSSeatSync.Domain.Venues;

public static class VenueErrors
{
    public static Error DuplicateSeat(SeatPosition position) =>
        new("Venue.DuplicateSeat", $"The venue already has a seat at {position}.");
}
