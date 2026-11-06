using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using Testcontainers.PostgreSql;

namespace WLVSSeatSync.Api.IntegrationTests
{
    public sealed class SeatSyncApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public const string TestSigningKey = "integration-tests-only-signing-key-0123456789";

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            // Environment variables are read by WebApplication.CreateBuilder before any of
            // Program.cs runs, so AddNpgsqlDbContext sees the test database straight away.
            Environment.SetEnvironmentVariable("ConnectionStrings__seatsyncdb", _postgres.GetConnectionString());
            Environment.SetEnvironmentVariable("Jwt__SigningKey", TestSigningKey);
        }

        // Development runs the migrations and seeds the venues, events and demo users.
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development");

        async Task IAsyncLifetime.DisposeAsync()
        {
            await DisposeAsync();            // stop the API first
            await _postgres.DisposeAsync();  // then remove the container
        }
    }
}
