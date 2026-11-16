using MassTransit;

using Microsoft.Extensions.DependencyInjection;

using WLVSSeatSync.Notifications.Consumers;
using WLVSSeatSync.Notifications.Email;
using WLVSSeatSync.Notifications.Tickets;

namespace WLVSSeatSync.Notications.Tests;

public class DependencyFixture : IDisposable
{
    public ServiceProvider ServiceProvider { get; }

    public DependencyFixture()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEmailSender, FakeEmailSender>();
        services.AddSingleton<TicketPdfGenerator>();
        services.AddMassTransitTestHarness(opt => opt.AddConsumer<OrderPaidConsumer>());

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        ServiceProvider.Dispose();
    }
}
