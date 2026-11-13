using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Common;

namespace WLVSSeatSync.Application.Abstractions;

public interface IPaymentGateway
{
    Task<Result<PaymentReceipt>> ChargeAsync(Guid orderId, Money amount, string cardToken,
        CancellationToken cancellationToken);

    Task RefundAsync(string chargeId, CancellationToken cancellationToken);
}

public sealed record PaymentReceipt(string ChargeId);

public static class PaymentErrors
{
    public static readonly Error Declined = new("Payment.Declined",
        "The card was declined. Your seats are still held; try another card.");

    public static readonly Error Unavailable = new("Payment.Unavailable",
        "The payment provider isn't responding. Your seats are still held; try again in a moment.",
        ErrorType.Unavailable);
}
