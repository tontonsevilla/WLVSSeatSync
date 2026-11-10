using WLVSSeatSync.Domain.Common;

namespace WLVSSeatSync.Domain.Pricing;

public sealed record PriceQuote(Money BasePrice, Money FinalPrice, string? AppliedDiscount);
