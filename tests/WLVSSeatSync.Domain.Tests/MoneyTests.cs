using Shouldly;

using WLVSSeatSync.Domain.Common;

namespace WLVSSeatSync.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Money_with_the_same_amount_and_currency_is_equal() =>
        new Money(1_500, "php").ShouldBe(Money.Pesos(1_500));

    [Fact]
    public void Adding_different_currencies_throws() =>
        Should.Throw<InvalidOperationException>(() => Money.Pesos(100) + new Money(2, "USD"));
}
