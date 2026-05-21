using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using smart_receipt_api.Services;

namespace smart_receipt_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Tags("Reports")]
    public class ReportsController : BaseApiController
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        /// <summary>Downloads a CSV receipt report.</summary>
        /// <param name="start">Optional start date. Defaults to 90 days before the end date.</param>
        /// <param name="end">Optional end date. Defaults to today.</param>
        /// <response code="200">The CSV report file was returned.</response>
        [HttpGet("receipts.csv")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> DownloadCsv([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            return await DownloadReport(() => _reportService.CreateCsvAsync(GetUserId(), start, end));
        }

        /// <summary>Downloads an Excel receipt report.</summary>
        /// <param name="start">Optional start date. Defaults to 90 days before the end date.</param>
        /// <param name="end">Optional end date. Defaults to today.</param>
        /// <response code="200">The Excel report file was returned.</response>
        [HttpGet("receipts.xlsx")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> DownloadXlsx([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            return await DownloadReport(() => _reportService.CreateXlsxAsync(GetUserId(), start, end));
        }

        /// <summary>Downloads a PDF receipt report.</summary>
        /// <param name="start">Optional start date. Defaults to 90 days before the end date.</param>
        /// <param name="end">Optional end date. Defaults to today.</param>
        /// <response code="200">The PDF report file was returned.</response>
        [HttpGet("receipts.pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> DownloadPdf([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            return await DownloadReport(() => _reportService.CreatePdfAsync(GetUserId(), start, end));
        }

        private async Task<IActionResult> DownloadReport(Func<Task<ReportFileResult>> factory)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized();

                var report = await factory();
                return File(report.Content, report.ContentType, report.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create report.");
                return StatusCode(500, "Could not create report.");
            }
        }
    }
}
