using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Common;

namespace WLVSSeatSync.Api.IntegrationTests;

// Approves every card except "tok_declined", instantly and always the same way.
internal sealed class TestPaymentGateway : IPaymentGateway
{
    public Task<Result<PaymentReceipt>> ChargeAsync(Guid orderId, Money amount, string cardToken,
        CancellationToken cancellationToken) =>
        Task.FromResult(cardToken == "tok_declined"
            ? Result.Failure<PaymentReceipt>(PaymentErrors.Declined)
            : Result.Success(new PaymentReceipt($"ch_test_{orderId:N}")));

    public Task RefundAsync(string chargeId, CancellationToken cancellationToken) => Task.CompletedTask;
}
