using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.JsonWebTokens;

using WLVSSeatSync.Client;

namespace WLVSSeatSync.Web.Auth;

public static class SeatSyncSignIn
{
    public const string AccessTokenName = "access_token";

    public static (ClaimsPrincipal Principal, AuthenticationProperties Properties) Create(TokenResponse token)
    {
        // Read the claims the API put in the token. No validation is needed here: the website got
        // this token straight from the API over HTTPS. The API still validates it on every call.
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token.AccessToken);
        var identity = new ClaimsIdentity(jwt.Claims, CookieAuthenticationDefaults.AuthenticationScheme,
            nameType: "name", roleType: "role");

        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = token.ExpiresAt, // the cookie dies with the token
        };
        properties.StoreTokens([new AuthenticationToken { Name = AccessTokenName, Value = token.AccessToken }]);

        return (new ClaimsPrincipal(identity), properties);
    }

    public static Task SignInAsync(HttpContext httpContext, TokenResponse token)
    {
        var (principal, properties) = Create(token);
        return httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }
}
