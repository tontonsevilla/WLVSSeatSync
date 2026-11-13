using System.Net;

using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Events;
using WLVSSeatSync.Domain.Orders;

namespace WLVSSeatSync.Application.Orders;

public sealed record DeleteOrderCommand(Guid CustomerId, Guid OrderId) : IRequest<Result>;

internal sealed class DeleteOrderCommandHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<DeleteOrderCommand, Result>
{
    public async Task<Result> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.CustomerId == request.CustomerId,
                cancellationToken);

        if (order == null)
            return OrderErrors.NotFound(request.OrderId);
        
        var resultCancel = order.Cancel(clock.GetUtcNow());

        if (resultCancel.IsFailure)
            return resultCancel;

        foreach(var item in order.Items)
        {
            var eventEntity = await db.Events
                .Include(e => e.Seats)
                .FirstOrDefaultAsync(e => e.Id == order.EventId, cancellationToken);
            
            if (eventEntity == null)
                return EventErrors.NotFound(order.EventId);

            eventEntity.ReleaseHoldsFor(order.Id);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
