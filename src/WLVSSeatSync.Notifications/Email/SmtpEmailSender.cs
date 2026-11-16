using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Options;

using MimeKit;

namespace WLVSSeatSync.Notifications.Email;

internal sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendAsync(string toAddress, string toName, string subject, string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments, CancellationToken cancellationToken)
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

        using var client = new SmtpClient();
        // Mailpit runs locally without TLS. A real provider in Phase 10 uses SecureSocketOptions.StartTls.
        await client.ConnectAsync(smtp.Host, smtp.Port, SecureSocketOptions.None, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
