using WLVSSeatSync.Domain.Abstractions;

namespace WLVSSeatSync.Domain.Tickets;

public static class TicketErrors
{
    public static Error AlreadyUsed(string code) => new("Ticket.AlreadyUsed", $"This ticket ({code}) has already been used.");
    public static Error Cancelled(string code) => new("Ticket.Cancelled", $"This ticket ({code}) has been cancelled.");
}
