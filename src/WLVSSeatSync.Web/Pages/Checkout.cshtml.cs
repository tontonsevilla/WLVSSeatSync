using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using WLVSSeatSync.Client;

namespace WLVSSeatSync.Web.Pages;

[Authorize] // only logged-in users; everyone else goes to the login page
public sealed class CheckoutModel(ISeatSyncApi api) : PageModel
{
    public OrderDetails Order { get; private set; } = null!;
    public string? ErrorMessage { get; private set; }

    [BindProperty] public string CardToken { get; set; } = "tok_visa";

    public async Task<IActionResult> OnGetAsync(Guid orderId, CancellationToken cancellationToken) =>
        await LoadAsync(orderId, cancellationToken) ? Page() : NotFound();

    public async Task<IActionResult> OnPostAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var response = await api.PayOrderAsync(orderId, new PayOrderRequest(CardToken), cancellationToken);
        if (response.IsSuccessful)
        {
            TempData["Message"] = "Payment received. Your tickets are below. Enjoy the show!";
            return RedirectToPage("/Account/Tickets");
        }

        ErrorMessage = ApiErrors.Describe(response.Error as Refit.ApiException); // e.g. "The card was declined. Your seats are still held..."
        return await LoadAsync(orderId, cancellationToken) ? Page() : NotFound();
    }

    private async Task<bool> LoadAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var response = await api.GetOrderAsync(orderId, cancellationToken);
        if (!response.IsSuccessful || response.Content is null)
            return false; // includes other people's orders: the API says 404 for those

        Order = response.Content;
        return true;
    }
}