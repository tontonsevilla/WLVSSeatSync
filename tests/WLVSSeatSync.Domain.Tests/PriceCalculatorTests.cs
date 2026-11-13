using System;
using System.Collections.Generic;
using System.Text;

using Shouldly;

using WLVSSeatSync.Domain.Common;
using WLVSSeatSync.Domain.Pricing;

namespace WLVSSeatSync.Domain.Tests;

public class PriceCalculatorTests
{
    private static readonly DateTimeOffset Now = TestData.Now;
    private static readonly Money Base = Money.Pesos(1_500);

    private static readonly PriceCalculator Calculator = new(
    [
        new EarlyBirdRule(),
        new GroupDiscountRule(),
        new PromoCodeRule(new Dictionary<string, decimal> { ["SEATSYNC10"] = 0.10m, ["BIGGROUP20"] = 0.20m }),
    ]);

    private static PriceQuote Quote(int daysBefore, int seats = 1, string? promo = null) =>
        Calculator.Calculate(Base, new PricingContext(Now, Now.AddDays(daysBefore), seats, promo));

    [Fact]
    public void Early_bird_starts_exactly_30_days_before_the_event()
    {
        Quote(daysBefore: 30).FinalPrice.ShouldBe(Money.Pesos(1_350));
        Quote(daysBefore: 29).FinalPrice.ShouldBe(Base);
    }

    [Fact]
    public void Groups_of_four_or_more_get_5_percent_off()
    {
        Quote(daysBefore: 10, seats: 4).FinalPrice.ShouldBe(Money.Pesos(1_425));
        Quote(daysBefore: 10, seats: 3).FinalPrice.ShouldBe(Base);
    }

    [Fact]
    public void Promo_codes_ignore_case_and_unknown_codes_do_nothing()
    {
        Quote(daysBefore: 10, promo: "seatsync10").AppliedDiscount.ShouldBe("Promo code");
        Quote(daysBefore: 10, promo: "NOPE").AppliedDiscount.ShouldBeNull();
    }

    [Fact]
    public void Only_the_single_best_discount_applies()
    {
        // Early bird (10%), group (5%) and promo (20%) all qualify; only the 20% is used.
        var quote = Quote(daysBefore: 60, seats: 5, promo: "BIGGROUP20");

        quote.FinalPrice.ShouldBe(Money.Pesos(1_200));
        quote.AppliedDiscount.ShouldBe("Promo code");
    }
}
