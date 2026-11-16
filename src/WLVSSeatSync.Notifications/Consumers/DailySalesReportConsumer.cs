using MassTransit;

using WLVSSeatSync.Contracts;
using WLVSSeatSync.Notifications.Email;

namespace WLVSSeatSync.Notifications.Consumers;

public sealed class DailySalesReportConsumer(IEmailSender email) : IConsumer<DailySalesReportReady>
{
    public async Task Consume(ConsumeContext<DailySalesReportReady> context)
    {
        var report = context.Message;
        var body = EmailTemplates.SalesReport(report);

        foreach (var recipient in report.RecipientEmails)
        {
            await email.SendAsync(recipient, "SeatSync admin",
                subject: $"Sales report for {report.Day:MMM d, yyyy}",
                htmlBody: body, attachments: null, context.CancellationToken);
        }
    }
}
