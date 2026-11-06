using System.ComponentModel.DataAnnotations;
using System.Text;

using Microsoft.IdentityModel.Tokens;

namespace WLVSSeatSync.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; init; } = string.Empty;
    [Required] public string Audience { get; init; } = string.Empty;
    [Required, MinLength(32)] public string SigningKey { get; init; } = string.Empty;
    [Range(5, 1440)] public int ExpiresInMinutes { get; init; } = 60;

    public SymmetricSecurityKey GetSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
