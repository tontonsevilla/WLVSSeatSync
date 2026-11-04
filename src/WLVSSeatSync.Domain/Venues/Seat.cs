using WLVSSeatSync.Domain.Abstractions;

namespace WLVSSeatSync.Domain.Venues;

public sealed class Seat : Entity
{
    private Seat() { } // for EF Core

    internal Seat(Guid venueId, SeatPosition position)
    {
        VenueId = venueId;
        Position = position;
    }

    public Guid VenueId { get; private set; }
    public SeatPosition Position { get; private set; } = null!; // set by EF Core when loading
}
