namespace WLVSSeatSync.Client
{
    // ----- Auth -----
    public sealed record LoginRequest(string Email, string Password);
    public sealed record RegisterRequest(string Email, string Password, string DisplayName);
    public sealed record GoogleLoginRequest(string IdToken);
    public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);

    // ----- Events -----
    public sealed record PagedList<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
    {
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasNextPage => Page < TotalPages;
    }

    public sealed record EventSummary(Guid Id, string Title, DateTimeOffset StartsAt, string VenueName, string City,
        decimal LowestPrice, string Currency);

    public sealed record EventDetails(Guid Id, string Title, string Description, DateTimeOffset StartsAt,
        DateTimeOffset EndsAt, string Status, VenueInfo Venue, IReadOnlyList<TicketTierDetails> TicketTiers);

    public sealed record VenueInfo(Guid Id, string Name, string City);

    public sealed record TicketTierDetails(Guid Id, string Name, decimal Price, string Currency,
        int AvailableSeats, int TotalSeats);

    public sealed record SeatMapSeat(Guid EventSeatId, string Section, string Row, int Number,
        Guid TicketTierId, bool IsAvailable);

    // ----- Orders -----
    public sealed record PlaceOrderRequest(Guid EventId, IReadOnlyList<Guid> EventSeatIds, string? PromoCode = null);
    public sealed record PayOrderRequest(string CardToken);

    public sealed record PlacedOrder(Guid OrderId, decimal Total, string Currency, DateTimeOffset ExpiresAt,
        string? AppliedDiscount);

    public sealed record PaidOrder(Guid OrderId, string ChargeId, IReadOnlyList<string> TicketCodes);

    public sealed record OrderSummary(Guid Id, Guid EventId, string EventTitle, DateTimeOffset EventStartsAt,
        string Status, decimal Total, string Currency, int SeatCount, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

    public sealed record OrderDetails(Guid Id, Guid EventId, string EventTitle, string Status, decimal Total,
        string Currency, DateTimeOffset ExpiresAt, DateTimeOffset? PaidAt,
        IReadOnlyList<OrderSeat> Seats, IReadOnlyList<OrderTicket> Tickets);

    public sealed record OrderSeat(Guid EventSeatId, string Section, string Row, int Number, decimal Price);
    public sealed record OrderTicket(string Code, Guid EventSeatId, string Status);

    // ----- Errors: the ProblemDetails the API returns -----
    public sealed record ApiProblem(string? Title, string? Detail, int? Status,
        IReadOnlyDictionary<string, string[]>? Errors);

    // ----- Roles: the values of the token's "role" claim -----
    public static class SeatSyncRoles
    {
        public const string Organizer = "Organizer";
        public const string Admin = "Admin";
    }

    // ----- Venues -----
    public sealed record VenueSummary(Guid Id, string Name, string City, int SeatCount);
    public sealed record VenueDetails(Guid Id, string Name, string City, IReadOnlyList<VenueSection> Sections);
    public sealed record VenueSection(string Name, int Rows, int Seats);

    public sealed record CreateVenueSection(string Name, int Rows, int SeatsPerRow);
    public sealed record CreateVenueRequest(string Name, string City, IReadOnlyList<CreateVenueSection> Sections);
    public sealed record CreatedResponse(Guid Id);

    // ----- Organizer and admin -----
    public sealed record CreateTicketTier(string Name, string Section, decimal Price);
    public sealed record CreateEventRequest(Guid VenueId, string Title, string Description,
        DateTimeOffset StartsAt, DateTimeOffset EndsAt, IReadOnlyList<CreateTicketTier> Tiers);

    public sealed record ManagedEvent(Guid Id, string Title, DateTimeOffset StartsAt, string Status,
        string VenueName, int TotalSeats, int SeatsSold);

    public sealed record TierSales(string TierName, string Currency, int TotalSeats, int SeatsSold, decimal Revenue);
    public sealed record DailySales(DateTime Day, int Orders, int Tickets, decimal Revenue);
    public sealed record EventSalesReport(Guid EventId, string Title,
        IReadOnlyList<TierSales> Tiers, IReadOnlyList<DailySales> Days);

    public sealed record LiveSale(Guid EventId, int Tickets, decimal Amount, string Currency, DateTimeOffset PaidAt);

    public sealed record AuditEntry(DateTimeOffset At, string Action, string? UserEmail, Guid? EntityId, string Details);

    // ----- Reviews -----
    public sealed record AddReviewRequest(int Rating, string Comment);
    public sealed record ReviewItem(string AuthorName, int Rating, string Comment, DateTimeOffset CreatedAt);
    public sealed record EventReviews(double? AverageRating, int Count, IReadOnlyList<ReviewItem> Latest);
}
