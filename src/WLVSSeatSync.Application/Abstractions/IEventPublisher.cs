namespace WLVSSeatSync.Application.Abstractions;

// Publishes integration events. With the outbox, a message is only really sent once
// the handler's SaveChangesAsync commits, and never if it doesn't.
public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken) where T : class;
}
