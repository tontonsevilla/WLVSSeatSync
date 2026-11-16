namespace WLVSSeatSync.Notifications.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string FromAddress { get; set; } = "tickets@seatsync.dev";
    public string FromName { get; set; } = "SeatSync";
}
