using HomeMaids.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HomeMaids.Services;

public interface IInvoiceService
{
    byte[] GenerateInvoicePdf(Booking booking);
}

public class InvoiceService : IInvoiceService
{
    static InvoiceService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(Booking booking)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(11));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("HomeMaids").FontSize(24).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text("Hourly Maid Booking System").FontSize(10).FontColor(Colors.Grey.Darken1);
                    });
                    row.ConstantItem(180).AlignRight().Column(col =>
                    {
                        col.Item().Text("INVOICE").FontSize(20).Bold();
                        col.Item().Text($"#{booking.BookingNumber}").FontSize(11);
                        col.Item().Text($"Date: {booking.CreatedAt:yyyy-MM-dd}").FontSize(10);
                    });
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Item().PaddingBottom(10).Text("Customer").Bold();
                    col.Item().Text(booking.Customer?.FullName ?? "-");
                    col.Item().Text(booking.Customer?.Email ?? "");
                    col.Item().Text(booking.Address);

                    col.Item().PaddingTop(20).PaddingBottom(10).Text("Booking Details").Bold();
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                        });
                        t.Cell().Element(CellHeader).Text("Worker");
                        t.Cell().Element(CellValue).Text(booking.Worker?.FullName ?? "-");
                        t.Cell().Element(CellHeader).Text("Date");
                        t.Cell().Element(CellValue).Text(booking.BookingDate.ToString("yyyy-MM-dd"));
                        t.Cell().Element(CellHeader).Text("Time");
                        t.Cell().Element(CellValue).Text($"{booking.StartTime:hh\\:mm} - {booking.EndTime:hh\\:mm}");
                        t.Cell().Element(CellHeader).Text("Hours");
                        t.Cell().Element(CellValue).Text(booking.Hours.ToString());
                    });

                    col.Item().PaddingTop(20).Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                        });
                        t.Cell().Element(CellRight).Text("Subtotal");
                        t.Cell().Element(CellRight).Text($"{booking.SubTotal:N2}");
                        t.Cell().Element(CellRight).Text("Discount");
                        t.Cell().Element(CellRight).Text($"-{booking.DiscountAmount:N2}");
                        t.Cell().Element(CellRight).Text("Tax");
                        t.Cell().Element(CellRight).Text($"{booking.TaxAmount:N2}");
                        t.Cell().Element(c => c.PaddingVertical(6).BorderTop(1).BorderColor(Colors.Black))
                            .AlignRight().Text("Total").Bold();
                        t.Cell().Element(c => c.PaddingVertical(6).BorderTop(1).BorderColor(Colors.Black))
                            .AlignRight().Text($"{booking.TotalAmount:N2}").Bold();
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Thank you for choosing HomeMaids — ").FontSize(9);
                    t.Span("hourly home services made simple.").FontSize(9).Italic();
                });
            });
        });

        return doc.GeneratePdf();

        static IContainer CellHeader(IContainer c) => c.PaddingVertical(4).Background(Colors.Grey.Lighten3).PaddingHorizontal(6);
        static IContainer CellValue(IContainer c) => c.PaddingVertical(4).PaddingHorizontal(6);
        static IContainer CellRight(IContainer c) => c.PaddingVertical(4).PaddingHorizontal(6).AlignRight();
    }
}
