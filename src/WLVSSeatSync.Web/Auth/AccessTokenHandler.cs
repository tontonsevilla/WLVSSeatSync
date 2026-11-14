using System.Net.Http.Headers;

using Microsoft.AspNetCore.Authentication;

namespace WLVSSeatSync.Web.Auth;

public sealed class AccessTokenHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (accessor.HttpContext is { } httpContext)
        {
            var token = await httpContext.GetTokenAsync(SeatSyncSignIn.AccessTokenName);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
