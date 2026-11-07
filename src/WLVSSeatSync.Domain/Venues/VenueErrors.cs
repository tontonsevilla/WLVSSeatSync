using WLVSSeatSync.Domain.Abstractions;

namespace WLVSSeatSync.Domain.Venues;

public static class VenueErrors
{
    public static Error DuplicateSeat(SeatPosition position) =>
        new("Venue.DuplicateSeat", $"The venue already has a seat at {position}.");

    public static Error NotFound(Guid id) => Error.NotFound("Venue.NotFound", $"Venue {id} was not found.");
    public static Error SectionNotFound(string section) =>
        Error.Validation("Venue.SectionNotFound", $"The venue has no section named '{section}'.");
}
