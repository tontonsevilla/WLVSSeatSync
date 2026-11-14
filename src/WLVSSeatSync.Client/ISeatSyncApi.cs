using Refit;

namespace WLVSSeatSync.Client;

public interface ISeatSyncApi
{
    // ----- Auth -----
    [Post("/auth/login")]
    Task<IApiResponse<TokenResponse>> LoginAsync([Body] LoginRequest request, CancellationToken cancellationToken = default);

    [Post("/auth/register")]
    Task<IApiResponse> RegisterAsync([Body] RegisterRequest request, CancellationToken cancellationToken = default);

    [Post("/auth/google")]
    Task<IApiResponse<TokenResponse>> GoogleLoginAsync([Body] GoogleLoginRequest request,
        CancellationToken cancellationToken = default);

    // ----- Events -----
    [Get("/events")]
    Task<PagedList<EventSummary>> SearchEventsAsync(string? search, int page, int pageSize,
        CancellationToken cancellationToken = default);

    [Get("/events/{id}")]
    Task<IApiResponse<EventDetails>> GetEventAsync(Guid id, CancellationToken cancellationToken = default);

    [Get("/events/{id}/seats")]
    Task<IReadOnlyList<SeatMapSeat>> GetSeatMapAsync(Guid id, CancellationToken cancellationToken = default);

    // ----- Orders -----
    [Post("/orders")]
    Task<IApiResponse<PlacedOrder>> PlaceOrderAsync([Body] PlaceOrderRequest request,
        CancellationToken cancellationToken = default);

    [Post("/orders/{id}/pay")]
    Task<IApiResponse<PaidOrder>> PayOrderAsync(Guid id, [Body] PayOrderRequest request,
        CancellationToken cancellationToken = default);

    [Get("/orders/mine")]
    Task<IReadOnlyList<OrderSummary>> GetMyOrdersAsync(CancellationToken cancellationToken = default);

    [Get("/orders/{id}")]
    Task<IApiResponse<OrderDetails>> GetOrderAsync(Guid id, CancellationToken cancellationToken = default);
}
