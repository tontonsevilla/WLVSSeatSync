using Microsoft.AspNetCore.Components.Authorization;

namespace WLVSSeatSync.Dashboard.Auth;

// Scoped: one per circuit, so one per logged-in browser tab.
public sealed class AccessTokenProvider(AuthenticationStateProvider authenticationState)
{
    public async Task<string?> GetAsync()
    {
        var state = await authenticationState.GetAuthenticationStateAsync();
        return state.User.FindFirst(DashboardSignIn.AccessTokenClaim)?.Value;
    }
}
