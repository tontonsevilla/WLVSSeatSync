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

    [Delete("/orders/{id}")] 
    Task<IApiResponse> CancelOrderAsync(Guid id, CancellationToken cancellationToken = default);

    // ----- Venues -----
    [Get("/venues")]
    Task<IReadOnlyList<VenueSummary>> GetVenuesAsync(CancellationToken cancellationToken = default);

    [Get("/venues/{id}")]
    Task<VenueDetails> GetVenueAsync(Guid id, CancellationToken cancellationToken = default);

    [Post("/venues")]
    Task<IApiResponse<CreatedResponse>> CreateVenueAsync([Body] CreateVenueRequest request,
        CancellationToken cancellationToken = default);

    // ----- Organizer -----
    [Post("/events")]
    Task<IApiResponse<CreatedResponse>> CreateEventAsync([Body] CreateEventRequest request,
        CancellationToken cancellationToken = default);

    [Post("/events/{id}/publish")]
    Task<IApiResponse> PublishEventAsync(Guid id, CancellationToken cancellationToken = default);

    [Get("/manage/events")]
    Task<IReadOnlyList<ManagedEvent>> GetManagedEventsAsync(CancellationToken cancellationToken = default);

    [Get("/manage/events/{id}/sales")]
    Task<EventSalesReport> GetEventSalesAsync(Guid id, CancellationToken cancellationToken = default);

    // ----- Reviews -----
    [Get("/events/{id}/reviews")]
    Task<EventReviews> GetReviewsAsync(Guid id, CancellationToken cancellationToken = default);

    [Post("/events/{id}/reviews")]
    Task<IApiResponse> AddReviewAsync(Guid id, [Body] AddReviewRequest request,
        CancellationToken cancellationToken = default);

    // ----- Admin -----
    [Get("/admin/audit")]
    Task<IReadOnlyList<AuditEntry>> GetAuditLogAsync(string? action, int take,
        CancellationToken cancellationToken = default);
}
