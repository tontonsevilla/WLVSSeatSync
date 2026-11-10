using Refit;

namespace WLVSSeatSync.Infrastructure.Payments;

// Refit generates the HttpClient code for this interface at compile time.
public interface IFakePaymentsApi
{
    [Post("/charges")]
    Task<ChargeResponse> CreateChargeAsync([Body] ChargeRequest request,
        [Header("Idempotency-Key")] string idempotencyKey, CancellationToken cancellationToken);

    [Post("/charges/{chargeId}/refund")]
    Task RefundAsync(string chargeId, CancellationToken cancellationToken);
}

public sealed record ChargeRequest(decimal Amount, string Currency, string CardToken, string Reference);
public sealed record ChargeResponse(string Id, string Status, decimal Amount, string Currency);
