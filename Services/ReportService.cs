using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using smart_receipt_api.Models;
using System.Globalization;
using System.Text;

namespace smart_receipt_api.Services
{
    public class ReportService : IReportService
    {
        private const string CsvContentType = "text/csv";
        private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string PdfContentType = "application/pdf";

        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ReportFileResult> CreateCsvAsync(int userId, DateTime? start, DateTime? end)
        {
            var (startDate, endDate) = ResolveRange(start, end);
            var receipts = await GetReceiptsAsync(userId, startDate, endDate);

            var csv = new StringBuilder();
            csv.AppendLine("Date,Store,Category,Item Count,Total (\u20BA)");

            foreach (var receipt in receipts)
            {
                csv.AppendLine(string.Join(",",
                    EscapeCsv(receipt.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    EscapeCsv(receipt.StoreName),
                    EscapeCsv(receipt.Category?.Name ?? string.Empty),
                    receipt.Items.Count.ToString(CultureInfo.InvariantCulture),
                    receipt.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture)));
            }

            return new ReportFileResult(
                CreateFileName(startDate, endDate, "csv"),
                CsvContentType,
                Encoding.UTF8.GetBytes(csv.ToString()));
        }

        public async Task<ReportFileResult> CreateXlsxAsync(int userId, DateTime? start, DateTime? end)
        {
            var (startDate, endDate) = ResolveRange(start, end);
            var receipts = await GetReceiptsAsync(userId, startDate, endDate);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Receipts");

            var headers = new[] { "Date", "Store", "Category", "Item Count", "Total (\u20BA)" };
            for (var i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            }

            for (var i = 0; i < receipts.Count; i++)
            {
                var receipt = receipts[i];
                var row = i + 2;
                worksheet.Cell(row, 1).Value = receipt.Date;
                worksheet.Cell(row, 1).Style.DateFormat.Format = "yyyy-mm-dd";
                worksheet.Cell(row, 2).Value = receipt.StoreName;
                worksheet.Cell(row, 3).Value = receipt.Category?.Name ?? string.Empty;
                worksheet.Cell(row, 4).Value = receipt.Items.Count;
                worksheet.Cell(row, 5).Value = receipt.TotalAmount;
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "\u20BA #,##0.00";
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new ReportFileResult(
                CreateFileName(startDate, endDate, "xlsx"),
                XlsxContentType,
                stream.ToArray());
        }

        public async Task<ReportFileResult> CreatePdfAsync(int userId, DateTime? start, DateTime? end)
        {
            var (startDate, endDate) = ResolveRange(start, end);
            var receipts = await GetReceiptsAsync(userId, startDate, endDate);
            var total = receipts.Sum(r => r.TotalAmount);

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(32);
                    page.Size(PageSizes.A4);

                    page.Header().Column(column =>
                    {
                        column.Item().Text("SmartReceipt - Spending Report").FontSize(20).Bold();
                        column.Item().Text($"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}").FontSize(10).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(16).Column(column =>
                    {
                        column.Spacing(12);
                        column.Item().Text($"Total receipts: {receipts.Count}");
                        column.Item().Text($"Total spending: \u20BA {total:0.00}");

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.2f);
                            });

                            table.Header(header =>
                            {
                                HeaderCell(header, "Date");
                                HeaderCell(header, "Store");
                                HeaderCell(header, "Category");
                                HeaderCell(header, "Items");
                                HeaderCell(header, "Total (\u20BA)");
                            });

                            foreach (var receipt in receipts)
                            {
                                BodyCell(table, receipt.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                                BodyCell(table, receipt.StoreName);
                                BodyCell(table, receipt.Category?.Name ?? string.Empty);
                                BodyCell(table, receipt.Items.Count.ToString(CultureInfo.InvariantCulture));
                                BodyCell(table, receipt.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture));
                            }
                        });
                    });
                });
            }).GeneratePdf();

            return new ReportFileResult(
                CreateFileName(startDate, endDate, "pdf"),
                PdfContentType,
                pdf);
        }

        private async Task<List<Receipt>> GetReceiptsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            var inclusiveEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            return await _context.Receipts
                .Include(r => r.Category)
                .Include(r => r.Items)
                .Where(r => r.UserId == userId && r.Date >= startDate.Date && r.Date <= inclusiveEndDate)
                .OrderByDescending(r => r.Date)
                .ToListAsync();
        }

        private static (DateTime Start, DateTime End) ResolveRange(DateTime? start, DateTime? end)
        {
            var endDate = (end ?? DateTime.UtcNow).Date;
            var startDate = (start ?? endDate.AddDays(-90)).Date;

            if (startDate > endDate)
                (startDate, endDate) = (endDate, startDate);

            return (startDate, endDate);
        }

        private static string CreateFileName(DateTime startDate, DateTime endDate, string extension)
        {
            return $"smartreceipt-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.{extension}";
        }

        private static string EscapeCsv(string value)
        {
            if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
                return value;

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private static void HeaderCell(TableCellDescriptor header, string text)
        {
            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(text).Bold();
        }

        private static void BodyCell(TableDescriptor table, string text)
        {
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text);
        }
    }
}
