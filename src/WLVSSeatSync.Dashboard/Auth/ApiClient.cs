using Refit;

using WLVSSeatSync.Client;

namespace WLVSSeatSync.Dashboard.Auth;

public static class ApiClient
{
    public const string Name = "seatsync-api";

    // One client per circuit, carrying that user's token.
    public static ISeatSyncApi Create(IServiceProvider services)
    {
        var tokens = services.GetRequiredService<AccessTokenProvider>();

        // The shared pipeline from IHttpClientFactory: service discovery, resilience, pooled connections.
        var pipeline = services.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(Name);

        var http = new HttpClient(new BearerTokenHandler(tokens) { InnerHandler = pipeline }, disposeHandler: false)
        {
            BaseAddress = new Uri("https+http://api"),
        };

        return RestService.For<ISeatSyncApi>(http);
    }
}
