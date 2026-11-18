namespace WLVSSeatSync.Application.Abstractions;

public sealed record LiveSale(Guid EventId, int Tickets, decimal Amount, string Currency, DateTimeOffset PaidAt);

public interface ISalesNotifier
{
    Task SaleMadeAsync(LiveSale sale, CancellationToken cancellationToken);
}
