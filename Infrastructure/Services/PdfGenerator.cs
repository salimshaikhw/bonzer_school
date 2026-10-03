using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public static class PdfGenerator
    {
        public static byte[] GenerateSamplePdfBytes()
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.Content().Column(col =>
                    {
                        col.Item().Text("Sample Report").FontSize(20).Bold();
                        col.Item().Text($"Generated: {DateTime.UtcNow:u}");
                        col.Item().Text("This PDF was generated in memory and attached to the email.");
                    });
                });
            });

            using var ms = new MemoryStream();
            document.GeneratePdf(ms);
            return ms.ToArray();
        }
    }
}
