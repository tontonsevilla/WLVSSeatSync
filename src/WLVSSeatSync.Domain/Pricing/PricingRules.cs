namespace WLVSSeatSync.Domain.Pricing;

public sealed class EarlyBirdRule : IPricingRule
{
    public static readonly TimeSpan MinimumLeadTime = TimeSpan.FromDays(30);

    public string Name => "Early bird";

    public decimal DiscountRate(PricingContext context) =>
        context.EventStartsAt - context.Now >= MinimumLeadTime ? 0.10m : 0m;
}

public sealed class GroupDiscountRule : IPricingRule
{
    public const int MinimumSeats = 4;

    public string Name => "Group discount";

    public decimal DiscountRate(PricingContext context) =>
        context.SeatCount >= MinimumSeats ? 0.05m : 0m;
}

public sealed class PromoCodeRule : IPricingRule
{
    private readonly Dictionary<string, decimal> _codes;

    public PromoCodeRule(IReadOnlyDictionary<string, decimal> codes) =>
        _codes = new Dictionary<string, decimal>(codes, StringComparer.OrdinalIgnoreCase);

    public string Name => "Promo code";

    public decimal DiscountRate(PricingContext context) =>
        context.PromoCode is { } code && _codes.TryGetValue(code.Trim(), out var rate) ? rate : 0m;
}
