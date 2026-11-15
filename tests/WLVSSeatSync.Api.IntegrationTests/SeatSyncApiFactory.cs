using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Testcontainers.PostgreSql;
using Testcontainers.Redis;

using WLVSSeatSync.Application.Abstractions;

namespace WLVSSeatSync.Api.IntegrationTests
{
    public sealed class SeatSyncApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public const string TestSigningKey = "integration-tests-only-signing-key-0123456789";

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

        private readonly RedisContainer _redis = new RedisBuilder("redis:7.4-alpine").Build();

        public async Task InitializeAsync()
        {
            // Start both at once: each takes a few seconds, so this halves the wait.
            await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

            // Environment variables are read by WebApplication.CreateBuilder before any of
            // Program.cs runs, so AddNpgsqlDbContext sees the test database straight away.
            Environment.SetEnvironmentVariable("ConnectionStrings__seatsyncdb", _postgres.GetConnectionString());
            Environment.SetEnvironmentVariable("ConnectionStrings__redis", _redis.GetConnectionString());
            Environment.SetEnvironmentVariable("Jwt__SigningKey", TestSigningKey);
        }

        // Development runs the migrations and seeds the venues, events and demo users.
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            // Runs after Program.cs registered its services, so these replace them.
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPaymentGateway>();
                services.AddSingleton<IPaymentGateway, TestPaymentGateway>();
            });
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await DisposeAsync();            // stop the API first
            await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());  // then remove the container
        }
    }
}
