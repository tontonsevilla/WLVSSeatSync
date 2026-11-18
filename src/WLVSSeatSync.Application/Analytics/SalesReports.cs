namespace WLVSSeatSync.Application.Analytics;

public sealed record TierSales(string TierName, string Currency, int TotalSeats, int SeatsSold, decimal Revenue);

public sealed record DailySales(DateOnly Day, int Orders, int Tickets, decimal Revenue);

public sealed record EventSalesReport(Guid EventId, string Title,
    IReadOnlyList<TierSales> Tiers, IReadOnlyList<DailySales> Days);

// Reports are written in SQL, so Application only describes what it needs.
public interface ISalesAnalytics
{
    Task<IReadOnlyList<TierSales>> GetTierSalesAsync(Guid eventId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DailySales>> GetDailySalesAsync(Guid eventId, string timeZone, CancellationToken cancellationToken);
}