using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using WLVSSeatSync.Domain.Events;
using WLVSSeatSync.Domain.Venues;

namespace WLVSSeatSync.Infrastructure.Persistence.Configurations;

internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");
        builder.Property(e => e.Title).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(e => e.StartsAt);

        // Another aggregate: a foreign key only, no navigation property.
        builder.HasOne<Venue>().WithMany().HasForeignKey(e => e.VenueId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.TicketTiers).WithOne().HasForeignKey(t => t.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Seats).WithOne().HasForeignKey(s => s.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TicketTierConfiguration : IEntityTypeConfiguration<TicketTier>
{
    public void Configure(EntityTypeBuilder<TicketTier> builder)
    {
        builder.ToTable("TicketTiers");
        builder.Property(t => t.Name).HasMaxLength(100);
        builder.ComplexProperty(t => t.Price, price =>
        {
            price.Property(m => m.Amount).HasPrecision(12, 2);
            price.Property(m => m.Currency).HasMaxLength(3);
        });
    }
}

internal sealed class EventSeatConfiguration : IEntityTypeConfiguration<EventSeat>
{
    public void Configure(EntityTypeBuilder<EventSeat> builder)
    {
        builder.ToTable("EventSeats");
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        // The database's own guard: a venue seat appears only once per event.
        builder.HasIndex(s => new { s.EventId, s.SeatId }).IsUnique();
        // Makes the Phase 3 "find expired holds" query fast.
        builder.HasIndex(s => new { s.Status, s.HeldUntil });

        builder.HasOne<TicketTier>().WithMany().HasForeignKey(s => s.TicketTierId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Seat>().WithMany().HasForeignKey(s => s.SeatId).OnDelete(DeleteBehavior.Restrict);
    }
}
