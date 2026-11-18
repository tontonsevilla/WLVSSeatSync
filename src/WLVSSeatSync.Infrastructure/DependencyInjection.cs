using Hangfire;
using Hangfire.PostgreSql;

using MassTransit;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;

using Polly;

using Refit;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Application.Analytics;
using WLVSSeatSync.Application.Reviews;
using WLVSSeatSync.Domain.Pricing;
using WLVSSeatSync.Infrastructure.Analytics;
using WLVSSeatSync.Infrastructure.Documents;
using WLVSSeatSync.Infrastructure.Identity;
using WLVSSeatSync.Infrastructure.Messaging;
using WLVSSeatSync.Infrastructure.Payments;
using WLVSSeatSync.Infrastructure.Persistence;

namespace WLVSSeatSync.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<SeatSyncDbContext>("seatsyncdb");
        builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<SeatSyncDbContext>());
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IUserDirectory, UserDirectory>();
        builder.Services.AddScoped<ISalesAnalytics, DapperSalesAnalytics>();

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<SeatSyncDbContext>();

        // Payments: an adapter over the provider's HTTP API, with a resilience pipeline tuned for it.
        builder.Services.AddScoped<IPaymentGateway, FakePaymentGateway>();
        builder.Services
            //.AddRefitGeneratedClient<IFakePaymentsApi>()
            .AddRefitClient<IFakePaymentsApi>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("https+http://payments"))
            .AddResilienceHandler("payments", pipeline =>
            {
                pipeline.AddTimeout(TimeSpan.FromSeconds(10));    // the whole call, all retries included

                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,   // wait ~0.3s, ~0.6s, ~1.2s
                    UseJitter = true,                             // randomise, so clients don't retry in sync
                    Delay = TimeSpan.FromMilliseconds(300),
                });

                pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    FailureRatio = 0.5,                           // half the calls failing...
                    MinimumThroughput = 10,                       // ...out of at least 10...
                    BreakDuration = TimeSpan.FromSeconds(15),     // ...stops calls for 15s
                });

                pipeline.AddTimeout(TimeSpan.FromSeconds(2));     // each single attempt
            });

        // Background jobs: Hangfire, storing its jobs in PostgreSQL (in its own "hangfire" schema).
        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(storage => storage.UseNpgsqlConnection(
                builder.Configuration.GetConnectionString("seatsyncdb")
                ?? throw new InvalidOperationException("Connection string 'seatsyncdb' is missing."))));

        builder.Services.AddHangfireServer();

        // Pricing strategies. PriceCalculator receives all three as IEnumerable<IPricingRule>.
        builder.Services.AddSingleton<IPricingRule, EarlyBirdRule>();
        builder.Services.AddSingleton<IPricingRule, GroupDiscountRule>();
        builder.Services.AddSingleton<IPricingRule>(_ => new PromoCodeRule(
            builder.Configuration.GetSection("Pricing:PromoCodes").Get<Dictionary<string, decimal>>()
                ?? new Dictionary<string, decimal>()));
        builder.Services.AddSingleton<PriceCalculator>();

        // Caching: a small copy in memory (L1) in front of a shared copy in Redis (L2).
        builder.AddRedisDistributedCache("redis");          // HybridCache picks this up as L2
        builder.Services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(60),       // how long Redis keeps it
                LocalCacheExpiration = TimeSpan.FromSeconds(10), // how long each API's memory keeps it
            };
        });

        // Messaging: MassTransit over RabbitMQ, with a transactional outbox in PostgreSQL.
        builder.Services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter(); // queue names like "live-sales", matching Notifications
            bus.AddConsumer<LiveSalesConsumer>();

            bus.AddEntityFrameworkOutbox<SeatSyncDbContext>(outbox =>
            {
                outbox.UsePostgres();   // PostgreSQL-specific locking, so several API instances share one outbox safely
                outbox.UseBusOutbox();  // Publish() writes to the outbox table instead of sending straight away
            });

            bus.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(new Uri(builder.Configuration.GetConnectionString("messaging")
                    ?? throw new InvalidOperationException("Connection string 'messaging' is missing.")));
                rabbit.ConfigureEndpoints(context);
            });
        });

        builder.Services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        // Documents: reviews and the audit log, in MongoDB.
        builder.AddMongoDBClient("docsdb");
        builder.Services.AddScoped<IReviewStore, MongoReviewStore>();
        builder.Services.AddScoped<IAuditLog, MongoAuditLog>();
        builder.Services.AddHostedService<MongoIndexSetup>();

        return builder;
    }
}
