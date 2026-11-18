using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;

using WLVSSeatSync.Application.Abstractions;

namespace WLVSSeatSync.Api.Auth;

// Null inside Hangfire jobs, where there's no HTTP request: those entries show as "system".
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    public string? Email => accessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Email);
}
