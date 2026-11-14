using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

using Refit;

using WLVSSeatSync.Client;
using WLVSSeatSync.Web.Auth;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// ---------- Pages, and output caching stored in Redis ----------
builder.Services.AddRazorPages();
builder.AddRedisOutputCache("redis");

// ---------- The API client ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AccessTokenHandler>();
builder.Services
    .AddRefitClient<ISeatSyncApi>()
    .ConfigureHttpClient(client => client.BaseAddress = new Uri("https+http://api"))
    .AddHttpMessageHandler<AccessTokenHandler>()
    .AddStandardResilienceHandler(options =>
    {
        // Paying can take ~10s while the API retries the payment provider.
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);

        // Retry reads only. Retrying a POST could place the same order twice.
        options.Retry.ShouldHandle = args => ValueTask.FromResult(
            args.Outcome.Result?.RequestMessage?.Method == HttpMethod.Get
            && HttpClientResiliencePredicates.IsTransient(args.Outcome));
    });

// ---------- Authentication: a cookie for the browser, holding the API's token ----------
var authentication = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "seatsync";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.SlidingExpiration = false; // the cookie lives exactly as long as the API token
    });

// Google is optional: with no client ID configured, the site works the same, minus the button.
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
if (!string.IsNullOrEmpty(googleClientId))
{
    authentication.AddOpenIdConnect(GoogleSignIn.Scheme, "Google", options =>
    {
        options.Authority = "https://accounts.google.com";
        options.ClientId = googleClientId;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.CallbackPath = "/signin-google";
        options.Scope.Add("email"); // "openid" and "profile" are included by default
        options.Events.OnTokenValidated = GoogleSignIn.ExchangeForSeatSyncTokenAsync;
    });
}

builder.Services.AddAuthorization();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache(); // after authentication, so it knows who is logged in

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();