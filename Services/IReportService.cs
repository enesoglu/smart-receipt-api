namespace smart_receipt_api.Services
{
    public record ReportFileResult(string FileName, string ContentType, byte[] Content);

    public interface IReportService
    {
        Task<ReportFileResult> CreateCsvAsync(int userId, DateTime? start, DateTime? end);
        Task<ReportFileResult> CreateXlsxAsync(int userId, DateTime? start, DateTime? end);
        Task<ReportFileResult> CreatePdfAsync(int userId, DateTime? start, DateTime? end);
    }
}
