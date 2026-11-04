using Shouldly;

using WLVSSeatSync.Domain.Tickets;

namespace WLVSSeatSync.Domain.Tests;

public class TicketTests
{
    private static readonly DateTimeOffset Now = TestData.Now;

    private static Ticket NewTicket() => Ticket.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);

    [Fact]
    public void A_new_ticket_is_valid_and_has_a_unique_32_character_code()
    {
        var ticket = NewTicket();

        ticket.Status.ShouldBe(TicketStatus.Valid);
        ticket.Code.Length.ShouldBe(32);
        NewTicket().Code.ShouldNotBe(ticket.Code);
    }

    [Fact]
    public void A_ticket_can_be_scanned_only_once()
    {
        var ticket = NewTicket();
        var gateOpens = Now.AddDays(30);

        ticket.Scan(gateOpens).IsSuccess.ShouldBeTrue();
        ticket.Scan(gateOpens.AddMinutes(1)).Error.Code.ShouldBe("Ticket.AlreadyUsed");
        ticket.UsedAt.ShouldBe(gateOpens);
    }

    [Fact]
    public void A_cancelled_ticket_cannot_be_scanned()
    {
        var ticket = NewTicket();
        ticket.Cancel();

        ticket.Scan(Now.AddDays(30)).Error.Code.ShouldBe("Ticket.Cancelled");
    }
}
