using QRCoder;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

using WLVSSeatSync.Contracts;

using WLVSSeatSync.Notifications.Email;

namespace WLVSSeatSync.Notifications.Tickets;

// One A6 landscape page per ticket: event details on the left, QR code on the right.
public sealed class TicketPdfGenerator
{
    public byte[] Generate(OrderPaid order) =>
        Document.Create(document =>
        {
            foreach (var ticket in order.Tickets)
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A6.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(text => text.FontSize(10));

                    page.Content().Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Spacing(4);
                            column.Item().Text("SEATSYNC TICKET").FontSize(8).FontColor(Colors.Grey.Medium);
                            column.Item().Text(order.EventTitle).FontSize(14).Bold();
                            column.Item().Text(EmailTemplates.When(order.EventStartsAt));
                            column.Item().Text(order.VenueName);
                            column.Item().PaddingTop(8)
                                .Text($"{ticket.Section}, row {ticket.Row}, seat {ticket.Number}").FontSize(12).Bold();
                            column.Item().PaddingTop(8).Text(ticket.Code).FontSize(7).FontColor(Colors.Grey.Medium);
                        });

                        // The QR code holds only the ticket code. The gate scanner looks it up (Phase 7).
                        row.ConstantItem(120).Image(
                            PngByteQRCodeHelper.GetQRCode(ticket.Code, QRCodeGenerator.ECCLevel.Q, 10));
                    });
                });
            }
        }).GeneratePdf();
}