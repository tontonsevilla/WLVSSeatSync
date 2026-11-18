using Bunit;

using Shouldly;

using WLVSSeatSync.Client;
using WLVSSeatSync.Dashboard.Components.Venues;

namespace WLVSSeatSync.Dashboard.Tests;

// BunitContext supplies the renderer and services; each test gets a fresh one.
public sealed class VenuePreviewTests : BunitContext
{
    [Fact]
    public void Draws_one_seat_for_every_row_and_number()
    {
        var cut = Render<VenuePreview>(parameters => parameters
            .Add(p => p.Sections, [new CreateVenueSection("Floor", 3, 4), new CreateVenueSection("Balcony", 2, 5)]));

        cut.FindAll(".venue-seat").Count.ShouldBe(3 * 4 + 2 * 5);
        cut.Find("[data-testid=total]").TextContent.ShouldStartWith("22 seats");
    }

    [Fact]
    public void Labels_rows_with_letters()
    {
        var cut = Render<VenuePreview>(parameters => parameters
            .Add(p => p.Sections, [new CreateVenueSection("Floor", 3, 1)]));

        cut.FindAll(".venue-row-label").Select(label => label.TextContent).ShouldBe(["A", "B", "C"]);
    }

    [Fact]
    public void Never_draws_more_than_the_api_would_accept()
    {
        var cut = Render<VenuePreview>(parameters => parameters
            .Add(p => p.Sections, [new CreateVenueSection("Huge", 500, 500)]));

        cut.FindAll(".venue-seat").Count.ShouldBe(VenuePreview.MaxRows * VenuePreview.MaxSeatsPerRow);
    }
}
