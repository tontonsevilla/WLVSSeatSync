using System.Security.Claims;

using Google.Apis.Auth;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;

using WLVSSeatSync.Api.Auth;
using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Infrastructure.Identity;

namespace WLVSSeatSync.Api.Endpoints;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record GoogleLoginRequest(string IdToken);
public sealed record MeResponse(string Id, string? Email, IReadOnlyList<string> Roles);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/google", GoogleLoginAsync);
        group.MapGet("/me", GetMe).RequireAuthorization();

        return app;
    }

    private static async Task<Results<Ok, ValidationProblem>> RegisterAsync(
        RegisterRequest request, UserManager<ApplicationUser> users)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName.Trim(),
        };

        // Identity checks the email format, uniqueness and password rules.
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            var errors = created.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            return TypedResults.ValidationProblem(errors);
        }

        await users.AddToRoleAsync(user, Roles.Attendee); // new accounts are always attendees
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<TokenResponse>, UnauthorizedHttpResult>> LoginAsync(
        LoginRequest request, UserManager<ApplicationUser> users, TokenService tokens)
    {
        var user = await users.FindByEmailAsync(request.Email);

        // The same answer for "no such email" and "wrong password", so attackers
        // can't use this endpoint to discover which emails have accounts.
        if (user is null || !await users.CheckPasswordAsync(user, request.Password))
            return TypedResults.Unauthorized();

        var roles = await users.GetRolesAsync(user);
        return TypedResults.Ok(tokens.CreateToken(user, roles));
    }

    private static async Task<Results<Ok<TokenResponse>, UnauthorizedHttpResult>> GoogleLoginAsync(
    GoogleLoginRequest request, UserManager<ApplicationUser> users, TokenService tokens, IConfiguration configuration)
    {
        var clientId = configuration["Authentication:Google:ClientId"];
        if (string.IsNullOrEmpty(clientId))
            return TypedResults.Unauthorized(); // Google sign-in isn't configured

        GoogleJsonWebSignature.Payload payload;
        try
        {
            // Checks Google's signature, the expiry, and that the token was issued for OUR client ID.
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });
        }
        catch (InvalidJwtException)
        {
            return TypedResults.Unauthorized();
        }

        // Linking by email is only safe if Google has verified the address.
        if (!payload.EmailVerified)
            return TypedResults.Unauthorized();

        const string provider = "Google";
        var user = await users.FindByLoginAsync(provider, payload.Subject)
                   ?? await users.FindByEmailAsync(payload.Email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = payload.Email,
                Email = payload.Email,
                EmailConfirmed = true,
                DisplayName = payload.Name ?? payload.Email,
            };

            var created = await users.CreateAsync(user); // no password: Google vouches for them
            if (!created.Succeeded)
                return TypedResults.Unauthorized();

            await users.AddToRoleAsync(user, Roles.Attendee);
        }

        // Remember the Google account, so a later email change at Google still finds this user.
        if (await users.FindByLoginAsync(provider, payload.Subject) is null)
            await users.AddLoginAsync(user, new UserLoginInfo(provider, payload.Subject, provider));

        var roles = await users.GetRolesAsync(user);
        return TypedResults.Ok(tokens.CreateToken(user, roles));
    }

    private static Ok<MeResponse> GetMe(ClaimsPrincipal user) =>
        TypedResults.Ok(new MeResponse(
            user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? string.Empty,
            user.FindFirstValue(JwtRegisteredClaimNames.Email),
            user.FindAll(TokenService.RoleClaimType).Select(c => c.Value).ToList()));
}
