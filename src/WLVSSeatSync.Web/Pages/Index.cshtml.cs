using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

using WLVSSeatSync.Client;

namespace WLVSSeatSync.Web.Pages;

// One cached copy per search term and page number.
[OutputCache(Duration = 60, VaryByQueryKeys = new[] { "search", "p" })]
public sealed class IndexModel(ISeatSyncApi api) : PageModel
{
    private const int PageSize = 12;

    [BindProperty(SupportsGet = true)] public string? Search { get; set; }

    // "p", not "page": Razor Pages reserves "page" for its own routing.
    [BindProperty(SupportsGet = true, Name = "p")] public int PageNumber { get; set; } = 1;

    public PagedList<EventSummary> Results { get; private set; } = new([], 1, PageSize, 0);

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Results = await api.SearchEventsAsync(Search, Math.Max(1, PageNumber), PageSize, cancellationToken);
}