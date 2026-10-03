using Application.ViewModels.Payment;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace API.Services;

public static class ReceiptPdfGenerator
{
    public static byte[] Generate(PaymentReceiptDto receipt)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(1.2f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Text("Payment Receipt")
                    .SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2);

                page.Content()
                    .PaddingVertical(0.5f, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(6);

                        column.Item().Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Receipt No:").SemiBold();
                                c.Item().Text(receipt.PaymentId.ToString("N")[..8] + "...");
                                c.Item().Text("Date:").SemiBold();
                                c.Item().Text(receipt.PaymentDate?.ToString("yyyy-MM-dd") ?? receipt.CreatedAt?.ToString("yyyy-MM-dd") ?? "-");
                                c.Item().Text("Roll No:").SemiBold();
                                c.Item().Text(receipt.RollNumber ?? "-");
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Class Id:").SemiBold();
                                c.Item().Text(receipt.ClassId?.ToString("N")[..8] + "..." ?? "-");
                                c.Item().Text("Division Id:").SemiBold();
                                c.Item().Text(receipt.DivisionId?.ToString("N")[..8] + "..." ?? "-");
                                c.Item().Text("Branch:").SemiBold();
                                c.Item().Text(receipt.BranchName ?? "-");
                            });
                        });

                        column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        column.Item().Text("Payment details").SemiBold().FontSize(12);
                        column.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Amount:");
                            r.RelativeItem().Text(receipt.Amount?.ToString("N2") ?? "0.00");
                        });
                        column.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Method:");
                            r.RelativeItem().Text(receipt.PaymentMethod ?? "-");
                        });
                        if (!string.IsNullOrWhiteSpace(receipt.TransactionNumber))
                        {
                            column.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Transaction No:");
                                r.RelativeItem().Text(receipt.TransactionNumber);
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(receipt.BankName))
                        {
                            column.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Bank:");
                                r.RelativeItem().Text(receipt.BankName);
                            });
                        }

                        column.Item().PaddingTop(8).Text("Fee breakdown").SemiBold().FontSize(12);
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Fee head").SemiBold();
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Amount").SemiBold();
                            });
                            foreach (var line in receipt.FeeLines ?? Array.Empty<ReceiptFeeLineDto>())
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(line.FeeHeadName);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(line.Amount.ToString("N2"));
                            }
                        });

                        column.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Total paid:").SemiBold();
                            r.RelativeItem().AlignRight().Text(receipt.Amount?.ToString("N2") ?? "0.00").SemiBold();
                        });

                        if (!string.IsNullOrWhiteSpace(receipt.Remarks))
                        {
                            column.Item().PaddingTop(6).Text("Remarks:").SemiBold();
                            column.Item().Text(receipt.Remarks);
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text("Thank you. This is a computer-generated receipt.")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });

        using var stream = new MemoryStream();
        doc.GeneratePdf(stream);
        return stream.ToArray();
    }
}
