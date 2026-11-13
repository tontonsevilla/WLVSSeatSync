using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Events;
using WLVSSeatSync.Domain.Orders;
using WLVSSeatSync.Domain.Pricing;

namespace WLVSSeatSync.Application.Orders;

public sealed record PlaceOrderCommand(Guid CustomerId, Guid EventId, IReadOnlyList<Guid> EventSeatIds, string? PromoCode)
    : IRequest<Result<PlacedOrder>>;

public sealed record PlacedOrder(Guid OrderId, decimal Total, string Currency, DateTimeOffset ExpiresAt,
    string? AppliedDiscount);

internal sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(c => c.CustomerId).NotEmpty();
        RuleFor(c => c.EventId).NotEmpty();
        RuleFor(c => c.EventSeatIds)
            .NotEmpty()
            .Must(ids => ids.Count <= Order.MaxSeatsPerOrder)
                .WithMessage($"An order can have at most {Order.MaxSeatsPerOrder} seats.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("Each seat can appear only once.");
        RuleFor(c => c.PromoCode).MaximumLength(30);
    }
}

internal sealed class PlaceOrderCommandHandler(IApplicationDbContext db, PriceCalculator pricing, TimeProvider clock)
    : IRequestHandler<PlaceOrderCommand, Result<PlacedOrder>>
{
    public async Task<Result<PlacedOrder>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var seatIds = request.EventSeatIds;

        // Load the event with its tiers and ONLY the seats being bought, not all of them.
        var ev = await db.Events
            .Include(e => e.TicketTiers)
            .Include(e => e.Seats.Where(s => seatIds.Contains(s.Id)))
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

        if (ev is null)
            return EventErrors.NotFound(request.EventId);

        // Price each seat from its tier, then let the strategies pick the best discount.
        var context = new PricingContext(now, ev.StartsAt, seatIds.Count, request.PromoCode);
        var lines = new List<OrderLine>();
        string? appliedDiscount = null;

        foreach (var seat in ev.Seats)
        {
            var tier = ev.TicketTiers.First(t => t.Id == seat.TicketTierId);
            var quote = pricing.Calculate(tier.Price, context);
            lines.Add(new OrderLine(seat.Id, quote.FinalPrice));
            appliedDiscount ??= quote.AppliedDiscount;
        }

        var placed = Order.Place(ev.Id, request.CustomerId, lines, now);
        if (placed.IsFailure)
            return placed.Error;

        var order = placed.Value;

        // The domain checks every seat: unknown, already held, already sold, event started.
        var held = ev.HoldSeats(seatIds, order.Id, now);
        if (held.IsFailure)
            return held.Error;

        db.Orders.Add(order);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another buyer changed one of these seats between our read and our write.
            return OrderErrors.SeatsTaken;
        }

        return new PlacedOrder(order.Id, order.Total.Amount, order.Total.Currency, order.ExpiresAt, appliedDiscount);
    }
}
