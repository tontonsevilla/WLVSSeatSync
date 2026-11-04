using System.Security.Cryptography;

using WLVSSeatSync.Domain.Abstractions;

namespace WLVSSeatSync.Domain.Tickets;

public sealed class Ticket : AggregateRoot
{
    private Ticket() { } //for EF Core

    public Guid OrderId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid EventSeatId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public TicketStatus Status { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    public static Ticket Issue(Guid orderId, Guid eventId, Guid eventSeatId, DateTimeOffset now)
    {
        return new Ticket
        {
            OrderId = orderId,
            EventId = eventId,
            EventSeatId = eventSeatId,
            Code = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            Status = TicketStatus.Valid,
            IssuedAt = now
        };
    }

    public Result Scan(DateTimeOffset now)
    {
        if (Status == TicketStatus.Cancelled) return TicketErrors.Cancelled(Code);
        if (Status == TicketStatus.Used) return TicketErrors.AlreadyUsed(Code);
        Status = TicketStatus.Used;
        UsedAt = now;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status == TicketStatus.Used) return TicketErrors.AlreadyUsed(Code);
        if (Status == TicketStatus.Cancelled) return TicketErrors.Cancelled(Code);
        Status = TicketStatus.Cancelled;
        return Result.Success();
    }
}
