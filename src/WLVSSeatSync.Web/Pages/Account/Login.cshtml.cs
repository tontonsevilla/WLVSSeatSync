using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using WLVSSeatSync.Client;
using WLVSSeatSync.Web.Auth;

namespace WLVSSeatSync.Web.Pages.Account;

public sealed class LoginModel(ISeatSyncApi api, IAuthenticationSchemeProvider schemes) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public bool GoogleEnabled { get; private set; }
    public string? ErrorMessage { get; private set; }

    public sealed class InputModel
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    }

    public async Task OnGetAsync() => GoogleEnabled = await IsGoogleEnabledAsync();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        GoogleEnabled = await IsGoogleEnabledAsync();
        if (!ModelState.IsValid)
            return Page();

        var response = await api.LoginAsync(new LoginRequest(Input.Email, Input.Password), cancellationToken);
        if (!response.IsSuccessful || response.Content is null)
        {
            ErrorMessage = "Wrong email or password.";
            return Page();
        }

        await SeatSyncSignIn.SignInAsync(HttpContext, response.Content);
        return LocalRedirect(ReturnUrl is { } url && Url.IsLocalUrl(url) ? url : "/");
    }

    // The "Continue with Google" button: hands the browser over to Google.
    public IActionResult OnPostGoogle() =>
        Challenge(new AuthenticationProperties { RedirectUri = ReturnUrl is { } url && Url.IsLocalUrl(url) ? url : "/" },
            GoogleSignIn.Scheme);

    private async Task<bool> IsGoogleEnabledAsync() => await schemes.GetSchemeAsync(GoogleSignIn.Scheme) is not null;
}
