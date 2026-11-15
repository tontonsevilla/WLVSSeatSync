using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using WLVSSeatSync.Client;

namespace WLVSSeatSync.Web.Pages.Events;

public sealed class DetailsModel(ISeatSyncApi api, IConfiguration configuration) : PageModel
{
    public EventDetails Event { get; private set; } = null!;
    public IReadOnlyList<SeatMapSeat> Seats { get; private set; } = [];
    public string HubUrl { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }

    [BindProperty] public List<Guid> SelectedSeatIds { get; set; } = [];
    [BindProperty] public string? PromoCode { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken) =>
        await LoadAsync(id, cancellationToken) ? Page() : NotFound();

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Challenge(); // sends them to the login page, then back here

        if (SelectedSeatIds.Count == 0)
            ErrorMessage = "Pick at least one seat.";
        else
        {
            var response = await api.PlaceOrderAsync(
                new PlaceOrderRequest(id, SelectedSeatIds, string.IsNullOrWhiteSpace(PromoCode) ? null : PromoCode),
                cancellationToken);

            if (response.IsSuccessful && response.Content is not null)
                return RedirectToPage("/Checkout", new { orderId = response.Content.OrderId });

            ErrorMessage = ApiErrors.Describe(response.Error as Refit.ApiException); // e.g. "Someone else just took one of these seats..."
        }

        return await LoadAsync(id, cancellationToken) ? Page() : NotFound();
    }

    private async Task<bool> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        var details = await api.GetEventAsync(id, cancellationToken);
        if (!details.IsSuccessful || details.Content is null)
            return false;

        Event = details.Content;
        Seats = await api.GetSeatMapAsync(id, cancellationToken);

        // Aspire tells this app the API's address. The browser connects to the hub there directly.
        var apiUrl = configuration["services:api:https:0"] ?? configuration["services:api:http:0"] ?? "";
        HubUrl = $"{apiUrl.TrimEnd('/')}/hubs/seat-map";
        return true;
    }
}