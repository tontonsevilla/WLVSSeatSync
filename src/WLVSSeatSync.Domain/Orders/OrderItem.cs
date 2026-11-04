using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Common;

namespace WLVSSeatSync.Domain.Orders;

public sealed class OrderItem : Entity
{
    private OrderItem() { } // for EF Core

    internal OrderItem(Guid orderId, Guid eventSeatId, Money price)
    {
        OrderId = orderId;
        EventSeatId = eventSeatId;
        Price = price;
    }

    public Guid OrderId { get; private set; }
    public Guid EventSeatId { get; private set; }
    public Money Price { get; private set; } = null!;
}
