using MassTransit;

using QuestPDF.Infrastructure;

using WLVSSeatSync.Notifications.Email;
using WLVSSeatSync.Notifications.Tickets;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

// QuestPDF is free under its Community licence for individuals and small companies.
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<TicketPdfGenerator>();

builder.Services.AddMassTransit(bus =>
{
    bus.SetKebabCaseEndpointNameFormatter();      // queue names like "order-paid"
    bus.AddConsumers(typeof(Program).Assembly);   // every IConsumer<T> in this project

    bus.UsingRabbitMq((context, rabbit) =>
    {
        rabbit.Host(new Uri(builder.Configuration.GetConnectionString("messaging")
            ?? throw new InvalidOperationException("Connection string 'messaging' is missing.")));

        // A failing consumer is retried with growing gaps. After the last attempt the message
        // moves to an "_error" queue for a person to inspect, instead of being lost.
        rabbit.UseMessageRetry(retry => retry.Exponential(
            retryLimit: 5,
            minInterval: TimeSpan.FromSeconds(1),
            maxInterval: TimeSpan.FromMinutes(1),
            intervalDelta: TimeSpan.FromSeconds(5)));

        rabbit.ConfigureEndpoints(context);
    });
});

builder.Build().Run();