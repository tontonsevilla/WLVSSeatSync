using Shouldly;

using WLVSSeatSync.Domain.Common;
using WLVSSeatSync.Domain.Events;

namespace WLVSSeatSync.Domain.Tests
{
    internal static class TestData
    {
        // A fixed "now" makes every test repeatable.
        public static readonly DateTimeOffset Now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

        public static Event PublishedEvent(int seatCount = 3)
        {
            var ev = Event.Create("Test Concert", "An event for tests", Guid.NewGuid(),
                startsAt: Now.AddDays(30), endsAt: Now.AddDays(30).AddHours(3), now: Now).Value;

            var tier = ev.AddTicketTier("General Admission", Money.Pesos(1_500)).Value;
            var venueSeatIds = Enumerable.Range(0, seatCount).Select(_ => Guid.NewGuid()).ToList();

            ev.AssignSeats(tier.Id, venueSeatIds).IsSuccess.ShouldBeTrue();
            ev.Publish().IsSuccess.ShouldBeTrue();
            return ev;
        }

        public static Guid SeatId(this Event ev, int index) => ev.Seats.ElementAt(index).Id;
    }
}
