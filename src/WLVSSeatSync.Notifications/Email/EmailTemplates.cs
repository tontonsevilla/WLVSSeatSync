using System.Net;

using WLVSSeatSync.Contracts;

namespace WLVSSeatSync.Notifications.Email;

internal static class EmailTemplates
{
    private static readonly TimeSpan PhilippineTime = TimeSpan.FromHours(8);

    // Event titles and names come from users. Encode them, or a title like
    // "<script>..." becomes live HTML in someone's inbox.
    private static string E(string value) => WebUtility.HtmlEncode(value);

    public static string When(DateTimeOffset value) =>
        value.ToOffset(PhilippineTime).ToString("dddd, MMMM d, yyyy 'at' h:mm tt");

    public static string Tickets(OrderPaid order)
    {
        var seats = string.Concat(order.Tickets.Select(t =>
            $"<li>{E(t.Section)}, row {E(t.Row)}, seat {t.Number}</li>"));

        return $"""
            <p>Hi {E(order.CustomerName)},</p>
            <p>You're going to <strong>{E(order.EventTitle)}</strong> on {When(order.EventStartsAt)}
               at {E(order.VenueName)}.</p>
            <ul>{seats}</ul>
            <p>Your tickets are attached as a PDF. Show the QR code at the gate, on your phone or printed.</p>
            <p>Total paid: {E(order.Currency)} {order.Total:N2}</p>
            <p>See you there,<br>SeatSync</p>
            """;
    }

    public static string Reminder(EventReminderDue reminder) => $"""
    <p>Hi {E(reminder.CustomerName)},</p>
    <p>A reminder: <strong>{E(reminder.EventTitle)}</strong> is tomorrow, {When(reminder.EventStartsAt)},
       at {E(reminder.VenueName)}.</p>
    <p>Have your QR code ready at the gate. See you there!</p>
    """;

    public static string SalesReport(DailySalesReportReady report)
    {
        var rows = string.Concat(report.Events.Select(e =>
            $"<tr><td>{E(e.EventTitle)}</td><td align=\"right\">{e.TicketsSold}</td>" +
            $"<td align=\"right\">{E(e.Currency)} {e.Revenue:N2}</td></tr>"));

        return report.Events.Count == 0
            ? $"<p>No tickets were sold on {report.Day:MMMM d, yyyy}.</p>"
            : $"""
            <p>Sales for {report.Day:dddd, MMMM d, yyyy}:</p>
            <table cellpadding="6" border="1" style="border-collapse:collapse">
              <tr><th>Event</th><th>Tickets</th><th>Revenue</th></tr>
              {rows}
            </table>
            """;
    }
}