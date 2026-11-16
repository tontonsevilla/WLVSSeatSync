namespace WLVSSeatSync.Contracts;

// Published when an order is paid. Carries everything needed to email the tickets,
// so the Notifications service never has to call back into the API or its database.
public sealed record OrderPaid(
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,
    string CustomerName,
    Guid EventId,
    string EventTitle,
    DateTimeOffset EventStartsAt,
    string VenueName,
    decimal Total,
    string Currency,
    IReadOnlyList<PaidTicket> Tickets,
    DateTimeOffset PaidAt);

public sealed record PaidTicket(string Code, string Section, string Row, int Number);

// Published by the hourly job for every paid order whose event starts in about 24 hours.
public sealed record EventReminderDue(
    Guid OrderId,
    string CustomerEmail,
    string CustomerName,
    string EventTitle,
    DateTimeOffset EventStartsAt,
    string VenueName);

// Published by the nightly job with yesterday's sales.
public sealed record DailySalesReportReady(
    DateOnly Day,
    IReadOnlyList<string> RecipientEmails,
    IReadOnlyList<EventSales> Events);

public sealed record EventSales(string EventTitle, int TicketsSold, decimal Revenue, string Currency);