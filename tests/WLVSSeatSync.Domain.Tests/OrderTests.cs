using Shouldly;

using WLVSSeatSync.Domain.Common;
using WLVSSeatSync.Domain.Orders;

namespace WLVSSeatSync.Domain.Tests;

public class OrderTests
{
    private static readonly DateTimeOffset Now = TestData.Now;

    private static Order NewOrder() => Order.Place(Guid.NewGuid(), Guid.NewGuid(),
        [new OrderLine(Guid.NewGuid(), Money.Pesos(1_500)), new OrderLine(Guid.NewGuid(), Money.Pesos(2_000))],
        Now).Value;

    [Fact]
    public void Paying_in_time_marks_the_order_paid_and_raises_OrderPaid()
    {
        var order = NewOrder();

        var result = order.MarkPaid(Now.AddMinutes(5));

        result.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Paid);
        order.Total.ShouldBe(Money.Pesos(3_500));
        order.GetDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<OrderPaidDomainEvent>();
    }

    [Fact]
    public void Paying_after_10_minutes_fails()
    {
        var order = NewOrder();

        var result = order.MarkPaid(Now.AddMinutes(10));

        result.Error.ShouldBe(OrderErrors.Expired);
        order.Status.ShouldBe(OrderStatus.Pending);
    }

    [Fact]
    public void An_order_cannot_have_more_than_10_seats()
    {
        var lines = Enumerable.Range(0, 11)
            .Select(_ => new OrderLine(Guid.NewGuid(), Money.Pesos(500)))
            .ToList();

        var result = Order.Place(Guid.NewGuid(), Guid.NewGuid(), lines, Now);

        result.Error.ShouldBe(OrderErrors.TooManySeats);
    }
}
