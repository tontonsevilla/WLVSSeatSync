using Shouldly;

using WLVSSeatSync.Domain.Events;

namespace WLVSSeatSync.Domain.Tests;

public class SeatHoldTests
{
    private static readonly DateTimeOffset Now = TestData.Now;

    [Fact]
    public void Holding_available_seats_succeeds()
    {
        var ev = TestData.PublishedEvent();

        var result = ev.HoldSeats([ev.SeatId(0), ev.SeatId(1)], Guid.NewGuid(), Now);

        result.IsSuccess.ShouldBeTrue();
        ev.Seats.Count(s => s.Status == SeatStatus.Held).ShouldBe(2);
    }

    [Fact]
    public void A_seat_can_never_be_sold_twice()
    {
        var ev = TestData.PublishedEvent();
        var seat = ev.SeatId(0);
        var firstBuyer = Guid.NewGuid();
        ev.HoldSeats([seat], firstBuyer, Now);
        ev.ConfirmSeats([seat], firstBuyer, Now.AddMinutes(2));

        var result = ev.HoldSeats([seat], Guid.NewGuid(), Now.AddHours(1));

        result.Error.Code.ShouldBe("Event.SeatUnavailable");
    }

    [Fact]
    public void A_held_seat_is_unavailable_until_the_hold_expires_after_10_minutes()
    {
        var ev = TestData.PublishedEvent();
        var seat = ev.SeatId(0);
        ev.HoldSeats([seat], Guid.NewGuid(), Now);

        ev.HoldSeats([seat], Guid.NewGuid(), Now.AddMinutes(9)).IsFailure.ShouldBeTrue();
        ev.HoldSeats([seat], Guid.NewGuid(), Now.AddMinutes(10)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Seats_cannot_be_held_once_the_event_has_started()
    {
        var ev = TestData.PublishedEvent();

        var result = ev.HoldSeats([ev.SeatId(0)], Guid.NewGuid(), ev.StartsAt);

        result.Error.ShouldBe(EventErrors.AlreadyStarted);
    }

    [Fact]
    public void Confirming_after_the_hold_expired_fails()
    {
        var ev = TestData.PublishedEvent();
        var seat = ev.SeatId(0);
        var buyer = Guid.NewGuid();
        ev.HoldSeats([seat], buyer, Now);

        var result = ev.ConfirmSeats([seat], buyer, Now.AddMinutes(11));

        result.Error.ShouldBe(EventErrors.HoldExpired);
        ev.Seats.ElementAt(0).Status.ShouldNotBe(SeatStatus.Sold);
    }

    [Fact]
    public void A_failed_hold_changes_nothing()
    {
        var ev = TestData.PublishedEvent();
        ev.HoldSeats([ev.SeatId(0)], Guid.NewGuid(), Now);

        var result = ev.HoldSeats([ev.SeatId(1), ev.SeatId(0)], Guid.NewGuid(), Now.AddMinutes(1));

        result.IsFailure.ShouldBeTrue();
        ev.Seats.ElementAt(1).Status.ShouldBe(SeatStatus.Available);
    }

    [Fact]
    public void Expired_holds_are_released()
    {
        var ev = TestData.PublishedEvent();
        ev.HoldSeats([ev.SeatId(0), ev.SeatId(1)], Guid.NewGuid(), Now);

        var released = ev.ReleaseExpiredHolds(Now.AddMinutes(10));

        released.ShouldBe(2);
        ev.Seats.ShouldAllBe(s => s.Status == SeatStatus.Available);
    }
}
