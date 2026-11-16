var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver(); // turns "https+http://api" into the real address

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapReverseProxy();

app.Run();