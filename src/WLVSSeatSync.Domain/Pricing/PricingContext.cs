namespace WLVSSeatSync.Domain.Pricing;

// Everything a rule may look at when deciding on a discount.
public sealed record PricingContext(DateTimeOffset Now, DateTimeOffset EventStartsAt, int SeatCount, string? PromoCode);
