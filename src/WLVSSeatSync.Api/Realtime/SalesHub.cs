using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using WLVSSeatSync.Api.Auth;
using WLVSSeatSync.Application.Abstractions;

namespace WLVSSeatSync.Api.Realtime;

public interface ISalesClient
{
    Task SaleMade(LiveSale sale);
}

// Unlike the public seat map, sales figures are for organizers and admins only.
[Authorize(Policy = Policies.ManageEvents)]
public sealed class SalesHub : Hub<ISalesClient>
{
    public static string GroupFor(Guid eventId) => $"sales-{eventId}";

    public Task WatchSales(Guid eventId) => Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(eventId));
}

internal sealed class SignalRSalesNotifier(IHubContext<SalesHub, ISalesClient> hub) : ISalesNotifier
{
    public Task SaleMadeAsync(LiveSale sale, CancellationToken cancellationToken) =>
        hub.Clients.Group(SalesHub.GroupFor(sale.EventId)).SaleMade(sale);
}
