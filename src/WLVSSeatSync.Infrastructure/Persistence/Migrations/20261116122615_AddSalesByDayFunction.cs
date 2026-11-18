using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WLVSSeatSync.Infrastructure.Persistence.Migrations
{
    public partial class AddSalesByDayFunction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION sales_by_day(p_event_id uuid, p_time_zone text)
                RETURNS TABLE ("Day" date, "Orders" integer, "Tickets" integer, "Revenue" numeric)
                LANGUAGE sql
                STABLE
                AS $$
                    SELECT (o."PaidAt" AT TIME ZONE p_time_zone)::date,
                           count(DISTINCT o."Id")::int,
                           count(oi."Id")::int,
                           coalesce(sum(oi."Price_Amount"), 0)::numeric(12, 2)
                    FROM "Orders" o
                    JOIN "OrderItems" oi ON oi."OrderId" = o."Id"
                    WHERE o."EventId" = p_event_id
                      AND o."Status" = 'Paid'
                    GROUP BY 1
                    ORDER BY 1;
                $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS sales_by_day(uuid, text);");
    }
}
