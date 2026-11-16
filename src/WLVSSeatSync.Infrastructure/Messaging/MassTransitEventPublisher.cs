using MassTransit;

using WLVSSeatSync.Application.Abstractions;

namespace WLVSSeatSync.Infrastructure.Messaging;

internal sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken) where T : class =>
        publishEndpoint.Publish(message, cancellationToken);
}
