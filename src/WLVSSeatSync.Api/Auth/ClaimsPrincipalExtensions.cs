using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;

namespace WLVSSeatSync.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : throw new InvalidOperationException("The token has no valid 'sub' claim.");
}
