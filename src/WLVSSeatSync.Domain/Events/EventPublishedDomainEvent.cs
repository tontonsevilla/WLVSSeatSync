using WLVSSeatSync.Domain.Abstractions;

namespace WLVSSeatSync.Domain.Events;

public sealed record EventPublishedDomainEvent(Guid EventId) : IDomainEvent;
