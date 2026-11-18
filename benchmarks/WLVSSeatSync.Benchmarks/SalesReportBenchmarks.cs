using BenchmarkDotNet.Attributes;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Npgsql;

using Testcontainers.PostgreSql;

using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Common;
using WLVSSeatSync.Domain.Events;
using WLVSSeatSync.Domain.Orders;
using WLVSSeatSync.Domain.Venues;
using WLVSSeatSync.Infrastructure.Analytics;
using WLVSSeatSync.Infrastructure.Persistence;

namespace WLVSSeatSync.Benchmarks;

public sealed record TierSalesRow(string TierName, string Currency, int TotalSeats, int SeatsSold, decimal Revenue);

[DapperAot]
[MemoryDiagnoser] // report allocations too, not just time
[InProcess]       // run all three in this process, so the database is created and seeded once
public class SalesReportBenchmarks
{
    private static readonly PostgreSqlContainer Postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private static Guid _eventId;

    private DbContextOptions<SeatSyncDbContext> _options = null!;
    private NpgsqlDataSource _dataSource = null!;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        if (_eventId == Guid.Empty) // GlobalSetup runs once per benchmark; seed only the first time
        {
            await Postgres.StartAsync();
            _eventId = await SeedAsync(CreateOptions());
        }

        _options = CreateOptions();
        _dataSource = NpgsqlDataSource.Create(Postgres.GetConnectionString());
    }

    [GlobalCleanup]
    public async Task CleanupAsync() => await _dataSource.DisposeAsync();

    [Benchmark(Baseline = true, Description = "EF Core LINQ")]
    public async Task<List<TierSalesRow>> EfCoreLinq()
    {
        await using var db = new SeatSyncDbContext(_options);
        var seats = db.Set<EventSeat>();
        var items = db.Set<OrderItem>();

        var rows = await db.Set<TicketTier>()
            .AsNoTracking()
            .Where(t => t.EventId == _eventId)
            .Select(t => new TierSalesRow(
                t.Name,
                t.Price.Currency,
                seats.Count(s => s.TicketTierId == t.Id),
                seats.Count(s => s.TicketTierId == t.Id && s.Status == SeatStatus.Sold),
                items
                    .Where(i => seats.Any(s => s.Id == i.EventSeatId && s.TicketTierId == t.Id)
                             && db.Orders.Any(o => o.Id == i.OrderId && o.Status == OrderStatus.Paid))
                    .Sum(i => i.Price.Amount)))
            .ToListAsync();

        return [.. rows.OrderByDescending(r => r.Revenue)];
    }

    [Benchmark(Description = "EF Core raw SQL")]
    public async Task<List<TierSalesRow>> EfCoreRawSql()
    {
        await using var db = new SeatSyncDbContext(_options);
        return await db.Database
            .SqlQueryRaw<TierSalesRow>(DapperSalesAnalytics.TierSalesSql, new NpgsqlParameter("EventId", _eventId))
            .ToListAsync();
    }

    [Benchmark(Description = "Dapper")]
    public async Task<List<TierSalesRow>> DapperQuery()
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<TierSalesRow>(DapperSalesAnalytics.TierSalesSql, new { EventId = _eventId });
        return rows.AsList();
    }

    private static DbContextOptions<SeatSyncDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<SeatSyncDbContext>().UseNpgsql(Postgres.GetConnectionString()).Options;

    // 3 sections x 20 rows x 50 seats = 3,000 seats. 80% sold, two seats per order, over 30 days.
    // Built through the domain model, so the data obeys every rule the real app does.
    private static async Task<Guid> SeedAsync(DbContextOptions<SeatSyncDbContext> options)
    {
        await using var db = new SeatSyncDbContext(options);
        await db.Database.MigrateAsync();

        var now = DateTimeOffset.UtcNow;
        (string Section, decimal Price)[] sections = [("Floor", 5_000m), ("Lower Box", 3_000m), ("Upper Box", 1_500m)];

        var venue = Venue.Create("Benchmark Arena", "Pasay");
        foreach (var (section, _) in sections)
            for (var row = 0; row < 20; row++)
                for (var number = 1; number <= 50; number++)
                    Ok(venue.AddSeat(new SeatPosition(section, ((char)('A' + row)).ToString(), number)));

        var ev = Event.Create("Benchmark Night", "Seeded for benchmarks", venue.Id,
            now.AddDays(60), now.AddDays(60).AddHours(3), now).Value;
        foreach (var (section, price) in sections)
        {
            var tier = ev.AddTicketTier(section, Money.Pesos(price)).Value;
            Ok(ev.AssignSeats(tier.Id, [.. venue.Seats.Where(s => s.Position.Section == section).Select(s => s.Id)]));
        }
        Ok(ev.Publish());

        var prices = ev.TicketTiers.ToDictionary(t => t.Id, t => t.Price);
        var pairs = ev.Seats.Take(2_400).Chunk(2).ToList();
        var orders = new List<Order>();

        for (var i = 0; i < pairs.Count; i++)
        {
            var paidAt = now.AddDays(-(i % 30)).AddMinutes(-5);
            var placedAt = paidAt.AddMinutes(-2);
            var seatIds = pairs[i].Select(s => s.Id).ToList();

            var order = Order.Place(ev.Id, Guid.NewGuid(),
                [.. pairs[i].Select(s => new OrderLine(s.Id, prices[s.TicketTierId]))], placedAt).Value;
            Ok(ev.HoldSeats(seatIds, order.Id, placedAt));
            Ok(ev.ConfirmSeats(seatIds, order.Id, paidAt));
            Ok(order.MarkPaid(paidAt));
            orders.Add(order);
        }

        db.Venues.Add(venue);
        db.Events.Add(ev);
        db.Orders.AddRange(orders);
        await db.SaveChangesAsync();
        return ev.Id;
    }

    private static void Ok(Result result)
    {
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error.Message);
    }
}
