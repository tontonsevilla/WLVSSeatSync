using WLVSSeatSync.Domain.Abstractions;

namespace WLVSSeatSync.Domain.Venues;

public sealed class Venue : AggregateRoot
{
    private readonly List<Seat> _seats = [];

    private Venue() { } // for EF Core

    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public IReadOnlyCollection<Seat> Seats => _seats;

    public static Venue Create(string name, string city)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);

        return new Venue { Name = name.Trim(), City = city.Trim() };
    }

    public Result AddSeat(SeatPosition position)
    {
        if (_seats.Any(s => s.Position == position))
            return VenueErrors.DuplicateSeat(position);

        _seats.Add(new Seat(Id, position));
        return Result.Success();
    }
}
