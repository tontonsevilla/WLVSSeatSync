using MassTransit;

using WLVSSeatSync.Contracts;
using WLVSSeatSync.Notifications.Email;
using WLVSSeatSync.Notifications.Tickets;

namespace WLVSSeatSync.Notifications.Consumers;

public sealed class OrderPaidConsumer(
    TicketPdfGenerator pdfs,
    IEmailSender email,
    ILogger<OrderPaidConsumer> logger) : IConsumer<OrderPaid>
{
    public async Task Consume(ConsumeContext<OrderPaid> context)
    {
        var order = context.Message;
        var pdf = pdfs.Generate(order);

        await email.SendAsync(
            order.CustomerEmail,
            order.CustomerName,
            subject: $"Your tickets for {order.EventTitle}",
            htmlBody: EmailTemplates.Tickets(order),
            attachments: [new EmailAttachment($"seatsync-tickets-{order.OrderId:N}.pdf", pdf, "application/pdf")],
            context.CancellationToken);

        // Log the order ID, never the email address: logs are kept for a long time and widely read.
        logger.LogInformation("Emailed {TicketCount} tickets for order {OrderId}", order.Tickets.Count, order.OrderId);
    }
}