using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Tickets;

namespace WLVSSeatSync.Application.Reviews;

public sealed record AddReviewCommand(Guid EventId, Guid UserId, int Rating, string Comment) : IRequest<Result>;

internal sealed class AddReviewCommandValidator : AbstractValidator<AddReviewCommand>
{
    public AddReviewCommandValidator()
    {
        RuleFor(c => c.EventId).NotEmpty();
        RuleFor(c => c.Rating).InclusiveBetween(1, 5);
        RuleFor(c => c.Comment).NotNull().MaximumLength(1000);
    }
}

internal sealed class AddReviewCommandHandler(
    IApplicationDbContext db, IReviewStore reviews, IUserDirectory users, TimeProvider clock)
    : IRequestHandler<AddReviewCommand, Result>
{
    public async Task<Result> Handle(AddReviewCommand request, CancellationToken cancellationToken)
    {
        // The rule needs PostgreSQL (who bought what); the review itself goes to MongoDB.
        var hasTicket = await db.Tickets.AnyAsync(t =>
                t.EventId == request.EventId &&
                t.Status != TicketStatus.Cancelled &&
                db.Orders.Any(o => o.Id == t.OrderId && o.CustomerId == request.UserId),
            cancellationToken);

        if (!hasTicket)
            return ReviewErrors.NoTicket;

        var author = await users.FindAsync(request.UserId, cancellationToken);
        var review = new NewReview(request.EventId, request.UserId, author?.DisplayName ?? "SeatSync fan",
            request.Rating, request.Comment.Trim(), clock.GetUtcNow());

        if (!await reviews.AddAsync(review, cancellationToken))
            return ReviewErrors.AlreadyReviewed;

        return Result.Success();
    }
}
