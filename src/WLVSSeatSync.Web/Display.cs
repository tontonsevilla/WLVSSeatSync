namespace WLVSSeatSync.Web;

public static class Display
{
    private static readonly TimeSpan PhilippineTime = TimeSpan.FromHours(8); // no daylight saving

    public static string When(DateTimeOffset value) =>
        value.ToOffset(PhilippineTime).ToString("ddd, MMM d, yyyy 'at' h:mm tt");

    public static string Money(string currency, decimal amount) => $"{currency} {amount:N2}";
}