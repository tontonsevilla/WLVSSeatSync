using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Orders;
using WLVSSeatSync.Domain.Tickets;

namespace WLVSSeatSync.Application.Orders;

public sealed record PayOrderCommand(Guid CustomerId, Guid OrderId, string CardToken) : IRequest<Result<PaidOrder>>;

public sealed record PaidOrder(Guid OrderId, string ChargeId, IReadOnlyList<string> TicketCodes);

internal sealed class PayOrderCommandValidator : AbstractValidator<PayOrderCommand>
{
    public PayOrderCommandValidator()
    {
        RuleFor(c => c.CustomerId).NotEmpty();
        RuleFor(c => c.OrderId).NotEmpty();
        RuleFor(c => c.CardToken).NotEmpty().MaximumLength(100);
    }
}

internal sealed class PayOrderCommandHandler(IApplicationDbContext db, 
    IPaymentGateway payments, // Actual payment gateway
    TimeProvider clock,
    OrderPaidMessageFactory orderPaidMessages,
    IEventPublisher events)
    : IRequestHandler<PayOrderCommand, Result<PaidOrder>>
{
    public async Task<Result<PaidOrder>> Handle(PayOrderCommand request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.CustomerId == request.CustomerId, cancellationToken);

        if (order is null)
            return OrderErrors.NotFound(request.OrderId);

        var seatIds = order.GetEventSeatIds();
        var ev = await db.Events
            .Include(e => e.Seats.Where(s => seatIds.Contains(s.Id)))
            .FirstAsync(e => e.Id == order.EventId, cancellationToken);

        // 1. Domain changes in memory. If any rule says no, nobody has been charged.
        var paid = order.MarkPaid(now);
        if (paid.IsFailure)
            return paid.Error;

        var confirmed = ev.ConfirmSeats(seatIds, order.Id, now);
        if (confirmed.IsFailure)
            return confirmed.Error;

        // 1b. NEW: issue the tickets and build the message, still in memory and before charging,
        // so a failure here (say, a missing email address) can't leave a charged card behind.
        var tickets = seatIds.Select(seatId => Ticket.Issue(order.Id, order.EventId, seatId, now)).ToList();
        var orderPaid = await orderPaidMessages.CreateAsync(order, ev, tickets, now, cancellationToken);

        // 2. Charge. The order ID is the idempotency key, so retries can't charge twice.
        var receipt = await payments.ChargeAsync(order.Id, order.Total, request.CardToken, cancellationToken);
        if (receipt.IsFailure)
            return receipt.Error;

        // 3. NEW: save the order, seats, tickets AND the OrderPaid message in ONE transaction.
        db.Tickets.AddRange(tickets);
        await events.PublishAsync(orderPaid, cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // 4. Compensate, as in Phase 3. The outbox row was part of the failed transaction,
            // so no OrderPaid is ever sent for this attempt.
            var paidElsewhere = await db.Orders
                .AsNoTracking()
                .AnyAsync(o => o.Id == order.Id && o.Status == OrderStatus.Paid, CancellationToken.None);

            if (!paidElsewhere)
                await payments.RefundAsync(receipt.Value.ChargeId, CancellationToken.None);

            return OrderErrors.PaymentConflict;
        }

        return new PaidOrder(order.Id, receipt.Value.ChargeId, tickets.Select(t => t.Code).ToList());
    }
}
