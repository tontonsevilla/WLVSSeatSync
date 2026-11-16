using Microsoft.Extensions.Options;

using MimeKit;

using WLVSSeatSync.Notifications.Email;

namespace WLVSSeatSync.Notications.Tests
{
    internal sealed class FakeEmailSender(IOptions<SmtpOptions> options) : IEmailSender
    {
        public List<MimeMessage> SentMessages { get; } = new List<MimeMessage>();

        public Task SendAsync(string toAddress, string toName, string subject, string htmlBody, IReadOnlyList<EmailAttachment>? attachments, CancellationToken cancellationToken)
        {
            var smtp = options.Value;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
            message.To.Add(new MailboxAddress(toName, toAddress));
            message.Subject = subject;

            var body = new BodyBuilder { HtmlBody = htmlBody };
            foreach (var attachment in attachments ?? [])
                body.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
            message.Body = body.ToMessageBody();

            SentMessages.Add(message);

            return Task.CompletedTask;

        }
    }
}
