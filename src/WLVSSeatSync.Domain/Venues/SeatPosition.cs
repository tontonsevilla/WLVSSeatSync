namespace WLVSSeatSync.Domain.Venues;

public sealed record SeatPosition
{
    private SeatPosition() { } // for EF Core

    public SeatPosition(string section, string row, int number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        ArgumentException.ThrowIfNullOrWhiteSpace(row);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number);

        Section = section.Trim();
        Row = row.Trim().ToUpperInvariant();
        Number = number;
    }

    public string Section { get; private init; } = string.Empty;
    public string Row { get; private init; } = string.Empty;
    public int Number { get; private init; }

    public override string ToString() => $"{Section}, Row {Row}, Seat {Number}";
}
