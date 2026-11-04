using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Events;
using WLVSSeatSync.Domain.Orders;
using WLVSSeatSync.Domain.Tickets;
using WLVSSeatSync.Domain.Venues;

namespace WLVSSeatSync.Infrastructure.Persistence
{
    public sealed class SeatSyncDbContext(DbContextOptions<SeatSyncDbContext> options) : DbContext(options)
    {
        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Ticket> Tickets => Set<Ticket>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Picks up every IEntityTypeConfiguration class in this project.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SeatSyncDbContext).Assembly);

            // The domain creates its own IDs, so EF must never treat them as database-generated.
            var entityTypes = modelBuilder.Model.GetEntityTypes()
                .Where(t => typeof(Entity).IsAssignableFrom(t.ClrType))
                .ToList();

            foreach (var entityType in entityTypes)
            {
                modelBuilder.Entity(entityType.ClrType).Property(nameof(Entity.Id)).ValueGeneratedNever();
            }
        }
    }
}
