using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

using WLVSSeatSync.Client;

namespace WLVSSeatSync.Web.Auth;

public static class GoogleSignIn
{
    public const string Scheme = "Google";

    // Runs after Google has signed the person in and the website has checked Google's response.
    public static async Task ExchangeForSeatSyncTokenAsync(TokenValidatedContext context)
    {
        var idToken = context.TokenEndpointResponse?.IdToken ?? context.ProtocolMessage?.IdToken;
        if (string.IsNullOrEmpty(idToken))
        {
            context.Fail("Google returned no ID token.");
            return;
        }

        var api = context.HttpContext.RequestServices.GetRequiredService<ISeatSyncApi>();
        var response = await api.GoogleLoginAsync(new GoogleLoginRequest(idToken), context.HttpContext.RequestAborted);
        if (!response.IsSuccessful || response.Content is null)
        {
            context.Fail("SeatSync could not sign you in with Google.");
            return;
        }

        // Replace Google's identity with SeatSync's, so the cookie holds our token and roles.
        var (principal, properties) = SeatSyncSignIn.Create(response.Content);
        context.Principal = principal;

        var target = context.Properties ??= new AuthenticationProperties();
        target.IsPersistent = true;
        target.ExpiresUtc = properties.ExpiresUtc;
        target.StoreTokens(properties.GetTokens());
    }
}
