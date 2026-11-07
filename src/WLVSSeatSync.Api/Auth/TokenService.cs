using System.Security.Claims;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using WLVSSeatSync.Infrastructure.Identity;

namespace WLVSSeatSync.Api.Auth;

public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider clock)
{
    public const string RoleClaimType = "role";

    public TokenResponse CreateToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var jwt = options.Value;
        var now = clock.GetUtcNow();
        var expiresAt = now.AddMinutes(jwt.ExpiresInMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // unique token ID
        };
        claims.AddRange(roles.Select(role => new Claim(RoleClaimType, role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(jwt.GetSigningKey(), SecurityAlgorithms.HmacSha256),
        };

        return new TokenResponse(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}
