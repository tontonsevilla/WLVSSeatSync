using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using WLVSSeatSync.Domain.Venues;

namespace WLVSSeatSync.Infrastructure.Persistence.Configurations;

internal sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("Venues");
        builder.Property(v => v.Name).HasMaxLength(200);
        builder.Property(v => v.City).HasMaxLength(100);

        builder.HasMany(v => v.Seats)
            .WithOne()
            .HasForeignKey(s => s.VenueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("Seats");

        // A complex type is stored as columns in the same table: Position_Section, Position_Row, Position_Number.
        builder.ComplexProperty(s => s.Position, position =>
        {
            position.Property(p => p.Section).HasMaxLength(50);
            position.Property(p => p.Row).HasMaxLength(10);
        });
    }
}
