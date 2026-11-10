using Microsoft.Extensions.Logging;

using Polly.CircuitBreaker;
using Polly.Timeout;

using Refit;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Common;

namespace WLVSSeatSync.Infrastructure.Payments;

internal sealed class FakePaymentGateway(IFakePaymentsApi api, ILogger<FakePaymentGateway> logger)
    : IPaymentGateway
{
    public async Task<Result<PaymentReceipt>> ChargeAsync(Guid orderId, Money amount, string cardToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var charge = await api.CreateChargeAsync(
                new ChargeRequest(amount.Amount, amount.Currency, cardToken, Reference: orderId.ToString()),
                idempotencyKey: $"order-{orderId}",
                cancellationToken);

            return charge.Status == "succeeded"
                ? new PaymentReceipt(charge.Id)
                : PaymentErrors.Declined;
        }
        catch (Exception ex) when (IsProviderFailure(ex))
        {
            logger.LogWarning(ex, "Payment provider failed for order {OrderId}", orderId);
            return PaymentErrors.Unavailable;
        }
    }

    public async Task RefundAsync(string chargeId, CancellationToken cancellationToken)
    {
        try
        {
            await api.RefundAsync(chargeId, cancellationToken);
        }
        catch (Exception ex) when (IsProviderFailure(ex))
        {
            // Never lose track of money. In production this would page someone.
            logger.LogCritical(ex, "Refund of {ChargeId} failed and needs manual follow-up", chargeId);
        }
    }

    // Everything that means "the provider didn't give us a usable answer, even after retries".
    private static bool IsProviderFailure(Exception ex) =>
        ex is ApiException or HttpRequestException or BrokenCircuitException or TimeoutRejectedException;
}
