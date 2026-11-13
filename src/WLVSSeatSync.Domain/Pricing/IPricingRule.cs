namespace WLVSSeatSync.Domain.Pricing;

public interface IPricingRule
{
    string Name { get; }

    // 0 means "doesn't apply"; 0.10 means 10% off.
    decimal DiscountRate(PricingContext context);
}
