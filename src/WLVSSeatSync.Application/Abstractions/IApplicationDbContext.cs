using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Domain.Events;
using WLVSSeatSync.Domain.Orders;
using WLVSSeatSync.Domain.Tickets;
using WLVSSeatSync.Domain.Venues;

namespace WLVSSeatSync.Application.Abstractions
{
    public interface IApplicationDbContext
    {
        DbSet<Venue> Venues { get; }
        DbSet<Event> Events { get; }
        DbSet<Order> Orders { get; }
        DbSet<Ticket> Tickets { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
