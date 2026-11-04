namespace WLVSSeatSync.Domain.Common;

public sealed record Money
{
    public const string Php = "PHP";

    private Money() { } // for EF Core

    public Money(decimal amount, string currency)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));

        Amount = decimal.Round(amount, 2);
        Currency = currency.ToUpperInvariant();
    }

    public decimal Amount { get; private init; }
    public string Currency { get; private init; } = Php;

    public static Money Pesos(decimal amount) => new(amount, Php);
    public static Money Zero(string currency = Php) => new(0, currency);

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot add {left.Currency} and {right.Currency}.");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public override string ToString() => $"{Currency} {Amount:N2}";
}
