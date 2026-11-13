using WLVSSeatSync.Domain.Common;

namespace WLVSSeatSync.Domain.Pricing;

public sealed class PriceCalculator(IEnumerable<IPricingRule> rules)
{
    public PriceQuote Calculate(Money basePrice, PricingContext context)
    {
        var best = rules
            .Select(rule => (Rule: rule, Rate: rule.DiscountRate(context)))
            .OrderByDescending(x => x.Rate)
            .FirstOrDefault();

        if (best.Rate <= 0)
            return new PriceQuote(basePrice, basePrice, AppliedDiscount: null);

        var finalPrice = new Money(basePrice.Amount * (1 - best.Rate), basePrice.Currency);
        return new PriceQuote(basePrice, finalPrice, best.Rule.Name);
    }
}
