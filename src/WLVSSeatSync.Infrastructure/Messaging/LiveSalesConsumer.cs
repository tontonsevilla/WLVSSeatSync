using MassTransit;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Contracts;

namespace WLVSSeatSync.Infrastructure.Messaging;

// A second subscriber to OrderPaid. Notifications emails the tickets; this one updates open dashboards.
internal sealed class LiveSalesConsumer(ISalesNotifier notifier) : IConsumer<OrderPaid>
{
    public Task Consume(ConsumeContext<OrderPaid> context)
    {
        var paid = context.Message;
        var sale = new LiveSale(paid.EventId, paid.Tickets.Count, paid.Total, paid.Currency, paid.PaidAt);
        return notifier.SaleMadeAsync(sale, context.CancellationToken);
    }
}
