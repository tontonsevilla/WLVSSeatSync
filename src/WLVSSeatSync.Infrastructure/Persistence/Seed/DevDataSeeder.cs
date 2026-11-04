using Bogus;
using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Domain.Common;
using WLVSSeatSync.Domain.Events;
using WLVSSeatSync.Domain.Venues;

namespace WLVSSeatSync.Infrastructure.Persistence.Seed;

public static class DevDataSeeder
{
    private static readonly (string Name, string City)[] VenueNames =
    [
        ("Smart Araneta Coliseum", "Quezon City"),
        ("SM Mall of Asia Arena", "Pasay"),
        ("Philippine Arena", "Bocaue, Bulacan"),
        ("New Frontier Theater", "Quezon City"),
        ("The Theatre at Solaire", "Parañaque"),
        ("Samsung Performing Arts Theater", "Makati"),
        ("Waterfront Cebu City Hotel", "Cebu City"),
        ("SMX Convention Center Davao", "Davao City"),
        ("Baguio Convention Center", "Baguio"),
        ("Iloilo Convention Center", "Iloilo City"),
    ];

    private static readonly string[] Themes = ["Manila", "Summer", "Island", "Neon", "Sunset", "Pinoy", "Starlight", "Harbor"];
    private static readonly string[] Kinds = ["Concert", "Tech Summit", "Comedy Night", "Film Festival", "Food Fair", "Theater Play", "Esports Finals", "Dance Showcase"];

    // Each section becomes a ticket tier with this base price in pesos.
    private static readonly (string Section, decimal BasePrice)[] Sections =
    [
        ("VIP", 5_500m),
        ("Lower Box", 3_500m),
        ("Upper Box", 1_500m),
    ];

    public static async Task SeedAsync(SeatSyncDbContext db, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (await db.Venues.AnyAsync(cancellationToken))
            return; // already seeded

        Randomizer.Seed = new Random(2026); // the same "random" data on every machine
        var faker = new Faker();

        var venues = VenueNames.Select(v => CreateVenue(faker, v.Name, v.City)).ToList();
        var events = Enumerable.Range(0, 50)
            .Select(_ => CreateEvent(faker, faker.PickRandom(venues), now))
            .ToList();

        db.Venues.AddRange(venues);
        db.Events.AddRange(events);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static Venue CreateVenue(Faker faker, string name, string city)
    {
        var venue = Venue.Create(name, city);

        foreach (var (section, _) in Sections)
        {
            var rows = faker.Random.Int(4, 8);
            var seatsPerRow = faker.Random.Int(10, 20);

            for (var row = 0; row < rows; row++)
            {
                for (var number = 1; number <= seatsPerRow; number++)
                {
                    venue.AddSeat(new SeatPosition(section, ((char)('A' + row)).ToString(), number));
                }
            }
        }

        return venue;
    }

    private static Event CreateEvent(Faker faker, Venue venue, DateTimeOffset now)
    {
        // Times are stored in UTC. 02:00, 05:00, 10:00 and 11:00 UTC are 10 am, 1 pm, 6 pm and 7 pm in Manila.
        var startsAt = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero)
            .AddDays(faker.Random.Int(7, 180))
            .AddHours(faker.PickRandom(2, 5, 10, 11));

        var title = $"{faker.PickRandom(Themes)} {faker.PickRandom(Kinds)} {startsAt.Year}";
        var ev = Event.Create(title, faker.Lorem.Paragraph(), venue.Id,
            startsAt, startsAt.AddHours(faker.Random.Int(2, 5)), now).Value;

        var priceFactor = faker.Random.Decimal(0.8m, 1.5m);
        foreach (var (section, basePrice) in Sections)
        {
            var price = Math.Round(basePrice * priceFactor / 50m) * 50m; // to the nearest PHP 50
            var tier = ev.AddTicketTier(section, Money.Pesos(price)).Value;
            var seatIds = venue.Seats
                .Where(s => s.Position.Section == section)
                .Select(s => s.Id)
                .ToList();

            ev.AssignSeats(tier.Id, seatIds);
        }

        ev.Publish();
        return ev;
    }
}