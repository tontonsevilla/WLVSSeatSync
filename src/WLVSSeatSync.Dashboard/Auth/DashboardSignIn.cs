using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.JsonWebTokens;

using WLVSSeatSync.Client;

namespace WLVSSeatSync.Dashboard.Auth;

public static class DashboardSignIn
{
    public const string AccessTokenClaim = "access_token";

    public static (ClaimsPrincipal Principal, AuthenticationProperties Properties) Create(TokenResponse token)
    {
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token.AccessToken);
        var claims = jwt.Claims.Append(new Claim(AccessTokenClaim, token.AccessToken));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme,
            nameType: "name", roleType: "role");

        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = token.ExpiresAt, // the cookie dies with the token
        };

        return (new ClaimsPrincipal(identity), properties);
    }
}

public static class DashboardPolicies
{
    public const string ManageEvents = nameof(ManageEvents); // Organizer or Admin
    public const string Administer = nameof(Administer);     // Admin only
}
