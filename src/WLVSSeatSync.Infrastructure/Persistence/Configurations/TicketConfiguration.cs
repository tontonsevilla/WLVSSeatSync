using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using WLVSSeatSync.Domain.Events;
using WLVSSeatSync.Domain.Orders;
using WLVSSeatSync.Domain.Tickets;

namespace WLVSSeatSync.Infrastructure.Persistence.Configurations;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.Property(t => t.Code).HasMaxLength(32);
        builder.HasIndex(t => t.Code).IsUnique(); // the gate scanner looks tickets up by code
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<Order>().WithMany().HasForeignKey(t => t.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Event>().WithMany().HasForeignKey(t => t.EventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EventSeat>().WithMany().HasForeignKey(t => t.EventSeatId).OnDelete(DeleteBehavior.Restrict);
    }
}
