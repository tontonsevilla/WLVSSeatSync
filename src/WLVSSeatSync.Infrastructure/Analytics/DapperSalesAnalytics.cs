using Dapper;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Analytics;
using WLVSSeatSync.Infrastructure.Persistence;

namespace WLVSSeatSync.Infrastructure.Analytics;

public sealed class DapperSalesAnalytics(SeatSyncDbContext db) : ISalesAnalytics
{
    // Aliases are quoted to keep their case, and listed in the same order as TierSales' constructor:
    // Dapper fills a positional record by matching columns to constructor parameters in order.
    public const string TierSalesSql = """
        SELECT t."Name"                                                         AS "TierName",
               t."Price_Currency"                                               AS "Currency",
               count(DISTINCT es."Id")::int                                     AS "TotalSeats",
               (count(DISTINCT es."Id") FILTER (WHERE es."Status" = 'Sold'))::int AS "SeatsSold",
               coalesce(sum(oi."Price_Amount") FILTER (WHERE o."Status" = 'Paid'), 0)::numeric(12, 2) AS "Revenue"
        FROM "TicketTiers" t
        JOIN "EventSeats" es      ON es."TicketTierId" = t."Id"
        LEFT JOIN "OrderItems" oi ON oi."EventSeatId" = es."Id"
        LEFT JOIN "Orders" o      ON o."Id" = oi."OrderId"
        WHERE t."EventId" = @EventId
        GROUP BY t."Id", t."Name", t."Price_Currency"
        ORDER BY "Revenue" DESC
        """;

    public async Task<IReadOnlyList<TierSales>> GetTierSalesAsync(Guid eventId, CancellationToken cancellationToken)
    {
        // EF Core's own connection: Dapper opens it if needed and closes it afterwards.
        var connection = db.Database.GetDbConnection();
        var rows = await connection.QueryAsync<TierSales>(
            new CommandDefinition(TierSalesSql, new { EventId = eventId }, cancellationToken: cancellationToken));
        return [.. rows];
    }

    public async Task<IReadOnlyList<DailySales>> GetDailySalesAsync(Guid eventId, string timeZone,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var rows = await connection.QueryAsync<DailySales>(new CommandDefinition(
            "SELECT * FROM sales_by_day(@EventId, @TimeZone)",
            new { EventId = eventId, TimeZone = timeZone },
            cancellationToken: cancellationToken));
        return [.. rows];
    }
}