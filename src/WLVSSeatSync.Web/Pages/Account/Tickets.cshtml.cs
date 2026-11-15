using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

using WLVSSeatSync.Client;

namespace WLVSSeatSync.Web.Pages.Account;

[Authorize]
public sealed class TicketsModel(ISeatSyncApi api) : PageModel
{
    public IReadOnlyList<OrderSummary> Orders { get; private set; } = [];
    public IReadOnlyDictionary<Guid, OrderDetails> PaidOrders { get; private set; } = new Dictionary<Guid, OrderDetails>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Orders = await api.GetMyOrdersAsync(cancellationToken);

        // Fetch the ticket codes for the 10 latest paid orders, in parallel rather than one by one.
        var paid = Orders.Where(o => o.Status == "Paid").Take(10).ToList();
        var responses = await Task.WhenAll(paid.Select(o => api.GetOrderAsync(o.Id, cancellationToken)));

        PaidOrders = responses
            .Where(r => r.IsSuccessful && r.Content is not null)
            .Select(r => r.Content!)
            .ToDictionary(order => order.Id);
    }
}