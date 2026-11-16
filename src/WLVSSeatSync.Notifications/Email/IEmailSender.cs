namespace WLVSSeatSync.Notifications.Email;

public sealed record EmailAttachment(string FileName, byte[] Content, string ContentType);

public interface IEmailSender
{
    Task SendAsync(string toAddress, string toName, string subject, string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments, CancellationToken cancellationToken);
}
