using System.Security.Claims;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Scalar.AspNetCore;

using Serilog;

using WLVSSeatSync.Api.Auth;
using WLVSSeatSync.Api.Endpoints;
using WLVSSeatSync.Api.Errors;
using WLVSSeatSync.Api.Middleware;
using WLVSSeatSync.Api.OpenApi;
using WLVSSeatSync.Application;
using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------- Logging ----------
builder.Logging.ClearProviders();   // Serilog writes the console from now on
builder.AddServiceDefaults();       // OpenTelemetry, health checks, resilience
builder.Services.AddSerilog((services, logger) => logger
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {SourceContext}{NewLine}  {Message:lj}{NewLine}{Exception}"),
    writeToProviders: true);        // keep feeding OpenTelemetry, so logs still reach the Aspire dashboard

// ---------- Application and infrastructure ----------
builder.AddInfrastructure();
builder.Services.AddApplication();

// ---------- JSON and errors ----------
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.RespectNullableAnnotations = true;          // null for a non-nullable property -> 400
    options.SerializerOptions.RespectRequiredConstructorParameters = true; // missing record property -> 400
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ---------- Authentication and authorization ----------
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        bearer.MapInboundClaims = false; // keep claim names as written: "sub", "email", "role"
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = jwt.GetSigningKey(),
            NameClaimType = "name",
            RoleClaimType = TokenService.RoleClaimType,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.ManageEvents, policy => policy.RequireRole(Roles.Organizer, Roles.Admin))
    .AddPolicy(Policies.ManageVenues, policy => policy.RequireRole(Roles.Admin));

// ---------- Rate limiting ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(OrderEndpoints.RateLimitPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        // One bucket per user, or per IP address for anonymous callers.
        partitionKey: httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,                     // 5 orders...
            Window = TimeSpan.FromMinutes(1),    // ...per minute, per user
        }));
});

// ---------- API documentation ----------
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();

// ---------- Middleware pipeline: order matters ----------
app.UseMiddleware<CorrelationIdMiddleware>(); // first, so every response and log line has the ID
app.UseExceptionHandler();                    // catches anything thrown further down
app.UseStatusCodePages();                     // empty 401/403/404 responses become ProblemDetails
app.UseSerilogRequestLogging();               // one log line per request, with timing
app.UseAuthentication();                      // who are you?
app.UseAuthorization();                       // are you allowed?

// ---------- Rate limiting: Add after authentication ----------
app.UseRateLimiter();

// ---------- Endpoints ----------
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("WLVS SeatSync API"));
    await app.Services.MigrateAndSeedDatabaseAsync();
}

// ---------- Map dev created endpoints ----------
app.MapAuthEndpoints();
app.MapVenueEndpoints();
app.MapEventEndpoints();
app.MapOrderEndpoints();

app.Run();

// Lets the integration tests start the app with WebApplicationFactory<Program>.
public partial class Program { }