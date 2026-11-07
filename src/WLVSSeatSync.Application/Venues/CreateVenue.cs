using FluentValidation;

using MediatR;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Venues;

namespace WLVSSeatSync.Application.Venues;

public sealed record CreateVenueCommand(string Name, string City, IReadOnlyList<CreateVenueSection> Sections)
    : IRequest<Result<Guid>>;

public sealed record CreateVenueSection(string Name, int Rows, int SeatsPerRow);

internal sealed class CreateVenueCommandValidator : AbstractValidator<CreateVenueCommand>
{
    public CreateVenueCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.City).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Sections).NotEmpty().Must(HaveUniqueSectionNames);
        RuleForEach(c => c.Sections).ChildRules(section =>
        {
            section.RuleFor(s => s.Name).NotEmpty().MaximumLength(50);
            section.RuleFor(s => s.Rows).GreaterThan(0).LessThanOrEqualTo(26);
            section.RuleFor(s => s.SeatsPerRow).GreaterThan(0).LessThanOrEqualTo(100);
        });
    }

    private bool HaveUniqueSectionNames(IReadOnlyList<CreateVenueSection> sections)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var section in sections)
        {
            if (!names.Add(section.Name))
                return false;
        }
        return true;
    }
}

internal sealed class CreateVenueCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateVenueCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateVenueCommand request, CancellationToken cancellationToken)
    {
        var venue = Venue.Create(request.Name, request.City);

        foreach (var section in request.Sections)
        {
            for (var rowIndex = 0; rowIndex < section.Rows; rowIndex++)
            {
                var rowLetter = ((char)('A' + rowIndex)).ToString();
                for (var seatNumber = 1; seatNumber <= section.SeatsPerRow; seatNumber++)
                {
                    var position = new SeatPosition(section.Name, rowLetter, seatNumber);
                    var result = venue.AddSeat(position);
                    if (result.IsFailure)
                        return result.Error;
                }
            }
        }

        db.Venues.Add(venue);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(venue.Id);
    }
}