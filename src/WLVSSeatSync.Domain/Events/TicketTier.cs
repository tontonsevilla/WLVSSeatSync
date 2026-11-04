using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Common;

namespace WLVSSeatSync.Domain.Events;

public sealed class TicketTier : Entity
{
    private TicketTier() { } // for EF Core

    internal TicketTier(Guid eventId, string name, Money price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        EventId = eventId;
        Name = name.Trim();
        Price = price;
    }

    public Guid EventId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Money Price { get; private set; } = null!;
}
