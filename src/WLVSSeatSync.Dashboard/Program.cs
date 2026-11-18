using System.Net.Http.Headers;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Http.Resilience;

using WLVSSeatSync.Client;
using WLVSSeatSync.Dashboard.Auth;
using WLVSSeatSync.Dashboard.Components;

using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = builder.Environment.IsDevelopment())
    .AddHubOptions(options => options.EnableDetailedErrors = builder.Environment.IsDevelopment());

// ---------- Authentication: a cookie that carries the API's token ----------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "seatsync-dashboard";
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/access-denied";
        options.SlidingExpiration = false;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(DashboardPolicies.ManageEvents, p => p.RequireRole(SeatSyncRoles.Organizer, SeatSyncRoles.Admin))
    .AddPolicy(DashboardPolicies.Administer, p => p.RequireRole(SeatSyncRoles.Admin));
builder.Services.AddCascadingAuthenticationState();

// ---------- The API client ----------
builder.Services.AddHttpClient(ApiClient.Name)
    .AddStandardResilienceHandler(options =>
    {
        // Retry reads only, as on the website.
        options.Retry.ShouldHandle = args => ValueTask.FromResult(
            args.Outcome.Result?.RequestMessage?.Method == HttpMethod.Get
            && HttpClientResiliencePredicates.IsTransient(args.Outcome));
    });
builder.Services.AddScoped<AccessTokenProvider>();
builder.Services.AddScoped<ISeatSyncApi>(ApiClient.Create);

// ---------- /jobs: the API's Hangfire dashboard, reached through this app ----------
builder.Services.AddReverseProxy()
    .LoadFromMemory(
        [
            new RouteConfig
            {
                RouteId = "jobs",
                ClusterId = "api",
                AuthorizationPolicy = DashboardPolicies.Administer, // checked here, before forwarding
                Match = new RouteMatch { Path = "/jobs/{**rest}" },
            },
        ],
        [
            new ClusterConfig
            {
                ClusterId = "api",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["primary"] = new() { Address = "https+http://api" },
                },
            },
        ])
    .AddServiceDiscoveryDestinationResolver()
    .AddTransforms(transforms =>
    {
        // The API never sees the dashboard's cookie...
        transforms.AddRequestHeaderRemove("Cookie");

        // ...it gets the admin's token instead.
        transforms.AddRequestTransform(context =>
        {
            var token = context.HttpContext.User.FindFirst(DashboardSignIn.AccessTokenClaim)?.Value;
            if (token is not null)
                context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return ValueTask.CompletedTask;
        });
    });

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery(); // after authentication, as Blazor requires

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapPost("/account/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/account/login");
});
app.MapReverseProxy();

app.Run();