using System.Globalization;
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
    // Brand constants
    private const string CompanyNameEn = "Al Thiqa Global Cleaning Services";
    private const string CompanyNameAr = "الثقة العالمية لخدمات التنظيف";
    private const string CompanyPhone = "77005570";
    private const string CompanyInstagram = "@althiqaglobal.om";
    private const string CompanyLocation = "Seeb - Muscat - Oman";
    private const string ArabicFontFamily = "Cairo";

    private static readonly object _fontLock = new();
    private static bool _fontsRegistered;

    private readonly IWebHostEnvironment _env;

    public InvoiceService(IWebHostEnvironment env)
    {
        _env = env;
        EnsureFontsRegistered();
    }

    static InvoiceService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private void EnsureFontsRegistered()
    {
        if (_fontsRegistered) return;
        lock (_fontLock)
        {
            if (_fontsRegistered) return;
            var regularPath = Path.Combine(_env.WebRootPath, "fonts", "Cairo-Regular.ttf");
            var boldPath = Path.Combine(_env.WebRootPath, "fonts", "Cairo-Bold.ttf");
            if (File.Exists(regularPath))
            {
                using var fs = File.OpenRead(regularPath);
                QuestPDF.Drawing.FontManager.RegisterFontWithCustomName(ArabicFontFamily, fs);
            }
            if (File.Exists(boldPath))
            {
                using var fs = File.OpenRead(boldPath);
                QuestPDF.Drawing.FontManager.RegisterFontWithCustomName(ArabicFontFamily, fs);
            }
            _fontsRegistered = true;
        }
    }

    public byte[] GenerateInvoicePdf(Booking booking)
    {
        var englishCulture = CultureInfo.InvariantCulture;
        var logoPath = Path.Combine(_env.WebRootPath, "images", "logo", "al-thiqa-full.png");

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(36);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(11).FontFamily(ArabicFontFamily));

                // === HEADER ===
                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Row(row =>
                    {
                        // Logo
                        if (File.Exists(logoPath))
                        {
                            row.ConstantItem(140).Height(70).Image(logoPath).FitArea();
                        }
                        else
                        {
                            row.ConstantItem(140);
                        }

                        // Company name (bilingual)
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text(CompanyNameAr).FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                            c.Item().AlignRight().Text(CompanyNameEn).FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    headerCol.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Colors.Blue.Darken3);

                    headerCol.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("فاتورة / INVOICE").FontSize(18).Bold();
                            c.Item().Text($"#{booking.BookingNumber}").FontSize(11).FontColor(Colors.Grey.Darken2);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text(t =>
                            {
                                t.Span("التاريخ / Date: ").SemiBold();
                                t.Span(booking.CreatedAt.ToString("yyyy-MM-dd", englishCulture));
                            });
                            c.Item().AlignRight().Text(t =>
                            {
                                t.Span("الوقت / Time: ").SemiBold();
                                t.Span(booking.CreatedAt.ToString("HH:mm", englishCulture));
                            });
                        });
                    });
                });

                // === CONTENT ===
                page.Content().PaddingVertical(16).Column(col =>
                {
                    // Customer section
                    col.Item().PaddingBottom(6).Text("بيانات العميل / Customer").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                    col.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                    {
                        c.Item().Text(t =>
                        {
                            t.Span("الاسم / Name: ").SemiBold();
                            t.Span(booking.Customer?.FullName ?? "-");
                        });
                        c.Item().Text(t =>
                        {
                            t.Span("البريد / Email: ").SemiBold();
                            t.Span(booking.Customer?.Email ?? "-");
                        });
                        c.Item().Text(t =>
                        {
                            t.Span("الجوال / Phone: ").SemiBold();
                            t.Span(booking.Customer?.PhoneNumber ?? "-");
                        });
                        c.Item().Text(t =>
                        {
                            t.Span("العنوان / Address: ").SemiBold();
                            t.Span(booking.Address);
                        });
                    });

                    // Booking details
                    col.Item().PaddingTop(16).PaddingBottom(6).Text("تفاصيل الحجز / Booking Details").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                        });
                        AddRow(t, "العاملة / Worker", booking.Worker?.FullName ?? "-");
                        AddRow(t, "الخدمة / Service", booking.Service?.Name ?? "-");
                        AddRow(t, "التاريخ / Date", booking.BookingDate.ToString("yyyy-MM-dd", englishCulture));
                        AddRow(t, "الوقت / Time", $"{booking.StartTime:hh\\:mm} - {booking.EndTime:hh\\:mm}");
                        AddRow(t, "الساعات / Hours", booking.Hours.ToString());
                        if (booking.Type == BookingType.Monthly)
                        {
                            AddRow(t, "نوع الحجز / Type", "عقد شهري / Monthly Contract");
                            if (booking.MonthlyVisits.HasValue)
                                AddRow(t, "الزيارات / Visits", booking.MonthlyVisits.Value.ToString());
                            if (booking.ContractEndDate.HasValue)
                                AddRow(t, "نهاية العقد / End Date", booking.ContractEndDate.Value.ToString("yyyy-MM-dd", englishCulture));
                        }
                    });

                    // Pricing
                    col.Item().PaddingTop(16).PaddingBottom(6).Text("التسعير / Pricing").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                        });
                        AddPriceRow(t, "المجموع الفرعي / Subtotal", $"{booking.SubTotal:N3} OMR");
                        if (booking.DiscountAmount > 0)
                            AddPriceRow(t, "الخصم / Discount", $"-{booking.DiscountAmount:N3} OMR");
                        AddPriceRow(t, "الضريبة 5% / Tax", $"{booking.TaxAmount:N3} OMR");

                        t.Cell().Element(c => c.PaddingVertical(8).BorderTop(1.5f).BorderColor(Colors.Blue.Darken3))
                            .AlignRight().Text("الإجمالي / Total").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                        t.Cell().Element(c => c.PaddingVertical(8).BorderTop(1.5f).BorderColor(Colors.Blue.Darken3))
                            .AlignRight().Text($"{booking.TotalAmount:N3} OMR").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                    });

                    // Brand values strip
                    col.Item().PaddingTop(24).Background(Colors.Blue.Lighten5).Padding(12).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("الجودة").Bold().FontColor(Colors.Blue.Darken3);
                            c.Item().AlignCenter().Text("Quality").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("المرونة").Bold().FontColor(Colors.Blue.Darken3);
                            c.Item().AlignCenter().Text("Flexibility").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("الثقة").Bold().FontColor(Colors.Blue.Darken3);
                            c.Item().AlignCenter().Text("Trust").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });

                // === FOOTER ===
                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(8).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten1);
                    col.Item().PaddingTop(8).AlignCenter().Text(t =>
                    {
                        t.Span("شكراً لاختياركم ").FontSize(10);
                        t.Span(CompanyNameAr).FontSize(10).Bold().FontColor(Colors.Blue.Darken3);
                        t.Span(" — Thank you for choosing us").FontSize(10);
                    });
                    col.Item().PaddingTop(4).AlignCenter().Text(t =>
                    {
                        t.Span("الهاتف / Phone: ").FontSize(10).SemiBold();
                        t.Span(CompanyPhone).FontSize(10).FontColor(Colors.Blue.Darken3);
                        t.Span("    |    ").FontSize(10);
                        t.Span("Instagram: ").FontSize(10).SemiBold();
                        t.Span(CompanyInstagram).FontSize(10).FontColor(Colors.Blue.Darken3);
                    });
                    col.Item().PaddingTop(2).AlignCenter().Text(CompanyLocation).FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static void AddRow(QuestPDF.Fluent.TableDescriptor t, string label, string value)
    {
        t.Cell().Element(CellHeader).Text(label);
        t.Cell().Element(CellValue).Text(value);
    }

    private static void AddPriceRow(QuestPDF.Fluent.TableDescriptor t, string label, string value)
    {
        t.Cell().Element(CellRight).Text(label);
        t.Cell().Element(CellRight).Text(value);
    }

    private static IContainer CellHeader(IContainer c) => c.PaddingVertical(5).PaddingHorizontal(8).Background(Colors.Grey.Lighten3);
    private static IContainer CellValue(IContainer c) => c.PaddingVertical(5).PaddingHorizontal(8);
    private static IContainer CellRight(IContainer c) => c.PaddingVertical(5).PaddingHorizontal(8).AlignRight();
}
