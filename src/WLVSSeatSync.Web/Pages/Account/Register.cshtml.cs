using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using WLVSSeatSync.Client;
using WLVSSeatSync.Web.Auth;

namespace WLVSSeatSync.Web.Pages.Account;

public sealed class RegisterModel(ISeatSyncApi api) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; private set; }

    public sealed class InputModel
    {
        [Required, StringLength(100), Display(Name = "Your name")]
        public string DisplayName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        var registered = await api.RegisterAsync(
            new RegisterRequest(Input.Email, Input.Password, Input.DisplayName), cancellationToken);
        if (!registered.IsSuccessful)
        {
            ErrorMessage = ApiErrors.Describe(registered.Error as Refit.ApiException); // e.g. "Passwords must have at least one digit"
            return Page();
        }

        var login = await api.LoginAsync(new LoginRequest(Input.Email, Input.Password), cancellationToken);
        if (login.Content is null)
            return RedirectToPage("/Account/Login");

        await SeatSyncSignIn.SignInAsync(HttpContext, login.Content);
        TempData["Message"] = $"Welcome to SeatSync, {Input.DisplayName}!";
        return LocalRedirect(ReturnUrl is { } url && Url.IsLocalUrl(url) ? url : "/");
    }
}
