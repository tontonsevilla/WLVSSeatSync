using WLVSSeatSync.Domain.Abstractions;

namespace WLVSSeatSync.Domain.Orders;

public static class OrderErrors
{
    public static readonly Error Empty = new("Order.Empty", "An order needs at least one seat.");
    public static readonly Error TooManySeats = new("Order.TooManySeats", $"An order can have at most {Order.MaxSeatsPerOrder} seats.");
    public static readonly Error NotPending = new("Order.NotPending", "Only a pending order can be changed.");
    public static readonly Error Expired = new("Order.Expired", "The order expired before payment.");
    public static readonly Error NotYetExpired = new("Order.NotYetExpired", "The order has not expired yet.");
}
