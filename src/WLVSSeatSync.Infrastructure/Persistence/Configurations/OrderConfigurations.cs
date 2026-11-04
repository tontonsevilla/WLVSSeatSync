using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using WLVSSeatSync.Domain.Events;
using WLVSSeatSync.Domain.Orders;

namespace WLVSSeatSync.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(o => o.Total); // calculated in C#, not stored
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => new { o.Status, o.ExpiresAt });

        builder.HasOne<Event>().WithMany().HasForeignKey(o => o.EventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.ComplexProperty(i => i.Price, price =>
        {
            price.Property(m => m.Amount).HasPrecision(12, 2);
            price.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.HasOne<EventSeat>().WithMany().HasForeignKey(i => i.EventSeatId).OnDelete(DeleteBehavior.Restrict);
    }
}
