using MassTransit.Testing;

using Microsoft.Extensions.DependencyInjection;

using MimeKit;

using Shouldly;

using WLVSSeatSync.Contracts;
using WLVSSeatSync.Notifications.Email;

namespace WLVSSeatSync.Notications.Tests;

public class ConsumerTests : IClassFixture<DependencyFixture>
{
    private readonly ITestHarness _harness;
    private readonly FakeEmailSender? _fakeEmailSender;

    public ConsumerTests(DependencyFixture fixture)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        _harness = fixture.ServiceProvider.GetRequiredService<ITestHarness>();
        
        var _emailSender = fixture.ServiceProvider.GetRequiredService<IEmailSender>();

        if (_emailSender != null)
        {
            _fakeEmailSender = _emailSender as FakeEmailSender;
        }
    }

    [Fact]
    public async Task Test_Consumer_Processes_Message()
    {
        // Arrange
        await _harness.Start();

        // Act
        await _harness.Bus.Publish(new OrderPaid(
            OrderId: Guid.NewGuid(), 
            CustomerId: Guid.NewGuid(), 
            CustomerEmail: "attendee@seatsync.dev",
            CustomerName:"Andrea Attendee",
            EventId: Guid.NewGuid(),
            EventTitle: "Test Event",
            EventStartsAt: DateTime.UtcNow,
            VenueName: "Test Venue",
            Total: 100_00,
            Currency: "PHP",
            Tickets: new List<PaidTicket> 
            { 
                new PaidTicket(
                    Code: "Test Code",
                    Section: "Test Section",
                    Row: "Test Row",
                    Number: 1
                )
            },
            PaidAt: DateTime.UtcNow
        ));

        // Assert
        (await _harness.Consumed.Any<OrderPaid>()).ShouldBeTrue();
        _fakeEmailSender?.SentMessages.Count.ShouldBe(1);

        ReadOnlySpan<byte> pdfSignature = [0x25, 0x50, 0x44, 0x46];
        var mimeEntity = _fakeEmailSender?.SentMessages.First().Attachments.First();

        using var stream = new MemoryStream();
        var mimePart = mimeEntity as MimePart;
        
        mimePart.ShouldNotBeNull();

        if (mimePart != null && mimePart.Content != null)
        {
            mimePart.Content.DecodeTo(stream);
        }

        stream.ToArray().AsSpan().StartsWith(pdfSignature).ShouldBeTrue();
    }
}
