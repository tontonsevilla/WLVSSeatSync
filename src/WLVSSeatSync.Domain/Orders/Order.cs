using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Common;
using WLVSSeatSync.Domain.Events;

namespace WLVSSeatSync.Domain.Orders;

public sealed class Order : AggregateRoot
{
    public const int MaxSeatsPerOrder = 10;

    private readonly List<OrderItem> _items = [];

    private Order() { } // for EF Core

    public Guid EventId { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items;

    public Money Total => _items.Aggregate(
        Money.Zero(_items.FirstOrDefault()?.Price.Currency ?? Money.Php),
        (sum, item) => sum + item.Price);

    public IReadOnlyCollection<Guid> GetEventSeatIds() => [.. _items.Select(i => i.EventSeatId)];

    public static Result<Order> Place(Guid eventId, Guid customerId,
        IReadOnlyCollection<OrderLine> lines, DateTimeOffset now)
    {
        if (lines.Count == 0) return OrderErrors.Empty;
        if (lines.Count > MaxSeatsPerOrder) return OrderErrors.TooManySeats;

        var order = new Order
        {
            EventId = eventId,
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now + Event.HoldDuration, // the order lives as long as its seat hold
        };
        order._items.AddRange(lines.Select(line => new OrderItem(order.Id, line.EventSeatId, line.Price)));
        return order;
    }

    public Result MarkPaid(DateTimeOffset now)
    {
        if (Status != OrderStatus.Pending) return OrderErrors.NotPending;
        if (now >= ExpiresAt) return OrderErrors.Expired;

        Status = OrderStatus.Paid;
        PaidAt = now;
        Raise(new OrderPaidDomainEvent(Id, EventId, now));
        return Result.Success();
    }

    public Result Expire(DateTimeOffset now)
    {
        if (Status != OrderStatus.Pending) return OrderErrors.NotPending;
        if (now < ExpiresAt) return OrderErrors.NotYetExpired;

        Status = OrderStatus.Expired;
        return Result.Success();
    }
}
