using WLVSSeatSync.Domain.Common;

namespace WLVSSeatSync.Domain.Orders;

// What the buyer asks for: one seat at one price.
public sealed record OrderLine(Guid EventSeatId, Money Price);
