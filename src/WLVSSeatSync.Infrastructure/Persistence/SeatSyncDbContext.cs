using MassTransit;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Orders;
using WLVSSeatSync.Domain.Tickets;
using WLVSSeatSync.Domain.Venues;
using WLVSSeatSync.Infrastructure.Identity;

namespace WLVSSeatSync.Infrastructure.Persistence
{
    public sealed class SeatSyncDbContext(DbContextOptions<SeatSyncDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
    {
        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<Domain.Events.Event> Events => Set<Domain.Events.Event>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Ticket> Tickets => Set<Ticket>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Identity's tables: AspNetUsers, AspNetRoles, ...

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SeatSyncDbContext).Assembly);

            var entityTypes = modelBuilder.Model.GetEntityTypes()
                .Where(t => typeof(Entity).IsAssignableFrom(t.ClrType))
                .ToList();

            foreach (var entityType in entityTypes)
            {
                modelBuilder.Entity(entityType.ClrType).Property(nameof(Entity.Id)).ValueGeneratedNever();
            }

            // MassTransit's transactional outbox tables.
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();
        }
    }
}
