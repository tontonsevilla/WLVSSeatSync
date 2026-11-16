using MassTransit;

using WLVSSeatSync.Contracts;
using WLVSSeatSync.Notifications.Email;

namespace WLVSSeatSync.Notifications.Consumers;

public sealed class EventReminderConsumer(IEmailSender email) : IConsumer<EventReminderDue>
{
    public Task Consume(ConsumeContext<EventReminderDue> context) =>
        email.SendAsync(context.Message.CustomerEmail, context.Message.CustomerName,
            subject: $"Tomorrow: {context.Message.EventTitle}",
            htmlBody: EmailTemplates.Reminder(context.Message),
            attachments: null,
            context.CancellationToken);
}
