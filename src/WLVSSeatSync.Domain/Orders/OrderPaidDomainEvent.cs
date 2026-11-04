using WLVSSeatSync.Domain.Abstractions;

namespace WLVSSeatSync.Domain.Orders;

public sealed record OrderPaidDomainEvent(Guid OrderId, Guid EventId, DateTimeOffset PaidAt) : IDomainEvent;
