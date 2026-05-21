using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using smart_receipt_api.DTOs;
using smart_receipt_api.Models;
using smart_receipt_api.Services;

namespace smart_receipt_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Tags("Receipts")]
    public class ReceiptsController : BaseApiController
    {
        private readonly IReceiptService _receiptService;
        private readonly IOcrService _ocrService;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReceiptsController> _logger;

        public ReceiptsController(
            IReceiptService receiptService,
            IOcrService ocrService,
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<ReceiptsController> logger)
        {
            _receiptService = receiptService;
            _ocrService = ocrService;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>Lists receipts for the current user.</summary>
        /// <response code="200">Receipts were returned.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<ReceiptDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<ReceiptDto>>>> GetUserReceipts()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<List<ReceiptDto>> { Success = false, Message = "Unauthorized." });

                var receipts = await _receiptService.GetUserReceiptsAsync(userId);
                return Ok(new ApiResponse<List<ReceiptDto>> { Success = true, Data = receipts.Select(MapToDto).ToList() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get receipts.");
                return StatusCode(500, new ApiResponse<List<ReceiptDto>> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Gets a receipt by id.</summary>
        /// <param name="id">Receipt id.</param>
        /// <response code="200">The receipt was returned.</response>
        /// <response code="404">The receipt was not found.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<ReceiptDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ReceiptDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ReceiptDto>>> GetReceipt(int id)
        {
            try
            {
                var receipt = await _receiptService.GetReceiptByIdAsync(id);
                if (receipt == null)
                    return NotFound(new ApiResponse<ReceiptDto> { Success = false, Message = "Receipt not found." });

                var userId = GetUserId();
                if (receipt.UserId != userId)
                    return Forbid();

                return Ok(new ApiResponse<ReceiptDto> { Success = true, Data = MapToDto(receipt) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get receipt.");
                return StatusCode(500, new ApiResponse<ReceiptDto> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Creates a receipt for the current user.</summary>
        /// <param name="request">Receipt fields and items.</param>
        /// <response code="201">The receipt was created.</response>
        /// <response code="400">The request is invalid.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ReceiptDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<ReceiptDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<ReceiptDto>>> CreateReceipt(CreateReceiptRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<ReceiptDto> { Success = false, Message = "Unauthorized." });

                var errors = ValidateReceiptRequest(request.StoreName, request.TotalAmount, request.Date);
                if (errors.Count > 0)
                    return BadRequest(new ApiResponse<ReceiptDto> { Success = false, Message = "Validation failed.", Errors = errors });

                var createdReceipt = await _receiptService.CreateReceiptWithStoreAsync(request, userId);
                return CreatedAtAction(nameof(GetReceipt), new { id = createdReceipt.Id },
                    new ApiResponse<ReceiptDto> { Success = true, Message = "Receipt created.", Data = MapToDto(createdReceipt) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create receipt.");
                return StatusCode(500, new ApiResponse<ReceiptDto> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Updates a receipt and replaces its item list.</summary>
        /// <param name="id">Receipt id.</param>
        /// <param name="request">Updated receipt fields and items.</param>
        /// <response code="200">The receipt was updated.</response>
        /// <response code="404">The receipt was not found.</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<ReceiptDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ReceiptDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ReceiptDto>>> UpdateReceipt(int id, UpdateReceiptRequest request)
        {
            try
            {
                var userId = GetUserId();
                var existingReceipt = await _receiptService.GetReceiptByIdAsync(id);

                if (existingReceipt == null)
                    return NotFound(new ApiResponse<ReceiptDto> { Success = false, Message = "Receipt not found." });
                if (existingReceipt.UserId != userId)
                    return Forbid();

                var errors = ValidateReceiptRequest(request.StoreName, request.TotalAmount, request.Date);
                if (errors.Count > 0)
                    return BadRequest(new ApiResponse<ReceiptDto> { Success = false, Message = "Validation failed.", Errors = errors });

                existingReceipt.StoreName = request.StoreName.Trim();
                existingReceipt.Date = request.Date;
                existingReceipt.TotalAmount = request.TotalAmount;
                existingReceipt.PhotoUrl = request.PhotoUrl;
                existingReceipt.CategoryId = request.CategoryId;
                existingReceipt.StoreId = request.StoreId;

                existingReceipt.Items.Clear();
                foreach (var itemDto in request.Items)
                    existingReceipt.Items.Add(MapItemDtoToEntity(itemDto));

                var updatedReceipt = await _receiptService.UpdateReceiptAsync(existingReceipt);
                return Ok(new ApiResponse<ReceiptDto> { Success = true, Message = "Receipt updated.", Data = MapToDto(updatedReceipt) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update receipt.");
                return StatusCode(500, new ApiResponse<ReceiptDto> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Deletes a receipt.</summary>
        /// <param name="id">Receipt id.</param>
        /// <response code="200">The receipt was deleted.</response>
        /// <response code="404">The receipt was not found.</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteReceipt(int id)
        {
            try
            {
                var userId = GetUserId();
                var receipt = await _receiptService.GetReceiptByIdAsync(id);

                if (receipt == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Receipt not found." });
                if (receipt.UserId != userId)
                    return Forbid();

                await _receiptService.DeleteReceiptAsync(id);
                return Ok(new ApiResponse<object> { Success = true, Message = "Receipt deleted.", Data = null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete receipt.");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Filters receipts by store name.</summary>
        /// <param name="storeName">Store name fragment.</param>
        /// <response code="200">Matching receipts were returned.</response>
        [HttpGet("filter/store")]
        [ProducesResponseType(typeof(ApiResponse<List<ReceiptDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<ReceiptDto>>>> FilterByStore([FromQuery] string storeName)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<List<ReceiptDto>> { Success = false, Message = "Unauthorized." });

                var receipts = await _receiptService.FilterReceiptsByStoreAsync(userId, storeName);
                return Ok(new ApiResponse<List<ReceiptDto>> { Success = true, Data = receipts.Select(MapToDto).ToList() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to filter receipts by store.");
                return StatusCode(500, new ApiResponse<List<ReceiptDto>> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Filters receipts by inclusive date range.</summary>
        /// <param name="startDate">Start date.</param>
        /// <param name="endDate">End date, treated as end of day.</param>
        /// <response code="200">Matching receipts were returned.</response>
        [HttpGet("filter/date")]
        [ProducesResponseType(typeof(ApiResponse<List<ReceiptDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<ReceiptDto>>>> FilterByDate([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<List<ReceiptDto>> { Success = false, Message = "Unauthorized." });

                var receipts = await _receiptService.FilterReceiptsByDateAsync(userId, startDate, endDate);
                return Ok(new ApiResponse<List<ReceiptDto>> { Success = true, Data = receipts.Select(MapToDto).ToList() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to filter receipts by date.");
                return StatusCode(500, new ApiResponse<List<ReceiptDto>> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Searches receipts by store or item name.</summary>
        /// <param name="query">Search text. Empty returns all receipts.</param>
        /// <response code="200">Matching receipts were returned.</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<ReceiptDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<ReceiptDto>>>> SearchReceipts([FromQuery] string? query)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<List<ReceiptDto>> { Success = false, Message = "Unauthorized." });

                var receipts = await _receiptService.SearchReceiptsAsync(userId, query);
                return Ok(new ApiResponse<List<ReceiptDto>> { Success = true, Data = receipts.Select(MapToDto).ToList() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search receipts.");
                return StatusCode(500, new ApiResponse<List<ReceiptDto>> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Gets dashboard summary statistics.</summary>
        /// <response code="200">Statistics were returned.</response>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<DashboardStatsDto> { Success = false, Message = "Unauthorized." });

                var receipts = (await _receiptService.GetUserReceiptsAsync(userId)).ToList();
                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;
                var monthlyReceipts = receipts.Where(r => r.Date.Month == currentMonth && r.Date.Year == currentYear).ToList();
                var mostFrequentStore = receipts.GroupBy(r => r.StoreName).OrderByDescending(g => g.Count()).FirstOrDefault();

                var stats = new DashboardStatsDto
                {
                    TotalMonthlySpending = monthlyReceipts.Sum(r => r.TotalAmount),
                    AverageReceiptValue = receipts.Count > 0 ? receipts.Average(r => r.TotalAmount) : 0m,
                    MostFrequentStore = mostFrequentStore?.Key ?? "N/A",
                    MostFrequentStoreVisitCount = mostFrequentStore?.Count() ?? 0
                };

                return Ok(new ApiResponse<DashboardStatsDto> { Success = true, Data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get dashboard stats.");
                return StatusCode(500, new ApiResponse<DashboardStatsDto> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Gets daily spending totals for a month.</summary>
        /// <param name="year">Year.</param>
        /// <param name="month">Month.</param>
        /// <response code="200">Daily spending data was returned.</response>
        [HttpGet("daily-spending")]
        [ProducesResponseType(typeof(ApiResponse<List<DailySpendingDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<DailySpendingDto>>>> GetDailySpending([FromQuery] int year, [FromQuery] int month)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<List<DailySpendingDto>> { Success = false, Message = "Unauthorized." });

                var receipts = (await _receiptService.GetUserReceiptsAsync(userId)).ToList();
                var daysInMonth = DateTime.DaysInMonth(year, month);
                var dailySpending = new List<DailySpendingDto>();

                for (var day = 1; day <= daysInMonth; day++)
                {
                    var date = new DateTime(year, month, day);
                    dailySpending.Add(new DailySpendingDto
                    {
                        Date = date.ToString("yyyy-MM-dd"),
                        Amount = receipts.Where(r => r.Date.Date == date).Sum(r => r.TotalAmount)
                    });
                }

                return Ok(new ApiResponse<List<DailySpendingDto>> { Success = true, Data = dailySpending });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get daily spending.");
                return StatusCode(500, new ApiResponse<List<DailySpendingDto>> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Gets spending grouped by store.</summary>
        /// <response code="200">Store spending data was returned.</response>
        [HttpGet("store-stats")]
        [ProducesResponseType(typeof(ApiResponse<List<StoreSpendingDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<StoreSpendingDto>>>> GetStoreStats()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<List<StoreSpendingDto>> { Success = false, Message = "Unauthorized." });

                var receipts = await _receiptService.GetUserReceiptsAsync(userId);
                var storeData = receipts
                    .GroupBy(r => r.StoreName)
                    .Select(g => new StoreSpendingDto
                    {
                        StoreName = g.Key,
                        TotalSpending = g.Sum(r => r.TotalAmount),
                        ReceiptCount = g.Count()
                    })
                    .OrderByDescending(s => s.TotalSpending)
                    .ToList();

                return Ok(new ApiResponse<List<StoreSpendingDto>> { Success = true, Data = storeData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get store stats.");
                return StatusCode(500, new ApiResponse<List<StoreSpendingDto>> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Gets dashboard chart data, including spending by category.</summary>
        /// <response code="200">Dashboard data was returned.</response>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(ApiResponse<DashboardDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<DashboardDto>>> GetDashboard()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<DashboardDto> { Success = false, Message = "Unauthorized." });

                var receipts = (await _receiptService.GetUserReceiptsAsync(userId)).ToList();
                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;

                var dashboard = new DashboardDto
                {
                    TotalMonthlySpending = receipts
                        .Where(r => r.Date.Month == currentMonth && r.Date.Year == currentYear)
                        .Sum(r => r.TotalAmount),
                    MonthlyData = receipts
                        .GroupBy(r => new { r.Date.Year, r.Date.Month })
                        .Select(g => new MonthlySpendingDto
                        {
                            Month = g.Key.Month,
                            Year = g.Key.Year,
                            TotalSpending = g.Sum(r => r.TotalAmount)
                        })
                        .OrderBy(m => m.Year)
                        .ThenBy(m => m.Month)
                        .ToList(),
                    StoreData = receipts
                        .GroupBy(r => r.StoreName)
                        .Select(g => new StoreSpendingDto
                        {
                            StoreName = g.Key,
                            TotalSpending = g.Sum(r => r.TotalAmount),
                            ReceiptCount = g.Count()
                        })
                        .OrderByDescending(s => s.TotalSpending)
                        .ToList(),
                    CategoryData = receipts
                        .Where(r => r.CategoryId != null && r.Category != null)
                        .GroupBy(r => new { r.CategoryId, CategoryName = r.Category!.Name })
                        .Select(g => new CategorySpendingDto
                        {
                            CategoryId = g.Key.CategoryId!.Value,
                            CategoryName = g.Key.CategoryName,
                            TotalSpending = g.Sum(r => r.TotalAmount),
                            ReceiptCount = g.Count()
                        })
                        .OrderByDescending(c => c.TotalSpending)
                        .ToList()
                };

                return Ok(new ApiResponse<DashboardDto> { Success = true, Data = dashboard });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get dashboard.");
                return StatusCode(500, new ApiResponse<DashboardDto> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Scans a receipt image with OCR and returns typed parse guesses.</summary>
        /// <param name="file">Receipt image file.</param>
        /// <response code="200">OCR data was returned.</response>
        /// <response code="400">The file is missing or OCR failed.</response>
        [HttpPost("scan")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ScanResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ScanResultDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<ScanResultDto>>> ScanReceipt([FromForm] IFormFile file)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<ScanResultDto> { Success = false, Message = "Unauthorized." });

                if (file == null || file.Length == 0)
                    return BadRequest(new ApiResponse<ScanResultDto> { Success = false, Message = "Image file is required." });

                var extractedData = await _ocrService.ExtractReceiptDataAsync(file);
                if (extractedData.TryGetValue("error", out var error))
                    return BadRequest(new ApiResponse<ScanResultDto> { Success = false, Message = error });

                var rawText = extractedData.TryGetValue("rawText", out var text) ? text : string.Empty;
                var result = _receiptService.BuildScanResult(rawText);

                return Ok(new ApiResponse<ScanResultDto> { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scan receipt.");
                return StatusCode(500, new ApiResponse<ScanResultDto> { Success = false, Message = "An error occurred during OCR processing." });
            }
        }

        /// <summary>Uploads or replaces the photo for a receipt.</summary>
        /// <param name="id">Receipt id.</param>
        /// <param name="file">Receipt photo file.</param>
        /// <response code="200">The photo was saved.</response>
        /// <response code="400">The file is invalid.</response>
        /// <response code="404">The receipt was not found.</response>
        [HttpPost("{id:int}/photo")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ReceiptPhotoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ReceiptPhotoDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<ReceiptPhotoDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ReceiptPhotoDto>>> UploadPhoto(int id, [FromForm] IFormFile file)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<ReceiptPhotoDto> { Success = false, Message = "Unauthorized." });

                var receipt = await _receiptService.GetReceiptByIdAsync(id);
                if (receipt == null || receipt.UserId != userId)
                    return NotFound(new ApiResponse<ReceiptPhotoDto> { Success = false, Message = "Receipt not found." });

                var validationError = ValidateUpload(file);
                if (validationError != null)
                    return BadRequest(new ApiResponse<ReceiptPhotoDto> { Success = false, Message = validationError });

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"{Guid.NewGuid():N}{extension}";
                var relativeFolder = Path.Combine("uploads", "receipts", userId.ToString());
                var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var absoluteFolder = Path.Combine(webRoot, relativeFolder);

                Directory.CreateDirectory(absoluteFolder);

                var absolutePath = Path.Combine(absoluteFolder, fileName);
                await using (var stream = System.IO.File.Create(absolutePath))
                {
                    await file.CopyToAsync(stream);
                }

                var photoUrl = "/" + Path.Combine(relativeFolder, fileName).Replace("\\", "/");
                receipt.PhotoUrl = photoUrl;
                await _receiptService.UpdateReceiptAsync(receipt);

                return Ok(new ApiResponse<ReceiptPhotoDto>
                {
                    Success = true,
                    Message = "Photo uploaded.",
                    Data = new ReceiptPhotoDto { PhotoUrl = photoUrl }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload receipt photo.");
                return StatusCode(500, new ApiResponse<ReceiptPhotoDto> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Gets top spending items.</summary>
        /// <param name="limit">Maximum item count, clamped from 1 to 50.</param>
        /// <param name="year">Optional year.</param>
        /// <param name="month">Optional month.</param>
        /// <response code="200">Top items were returned.</response>
        [HttpGet("items/top")]
        [ProducesResponseType(typeof(ApiResponse<List<ItemAggregateDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<ItemAggregateDto>>>> GetTopItems([FromQuery] int limit = 10, [FromQuery] int? year = null, [FromQuery] int? month = null)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<List<ItemAggregateDto>> { Success = false, Message = "Unauthorized." });

                var items = await _receiptService.GetTopItemsAsync(userId, limit, year, month);
                return Ok(new ApiResponse<List<ItemAggregateDto>> { Success = true, Data = items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get top items.");
                return StatusCode(500, new ApiResponse<List<ItemAggregateDto>> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Gets one short item spending insight for the last 30 days.</summary>
        /// <response code="200">The insight was returned.</response>
        [HttpGet("items/insights")]
        [ProducesResponseType(typeof(ApiResponse<InsightDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<InsightDto>>> GetItemInsights()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<InsightDto> { Success = false, Message = "Unauthorized." });

                var insight = await _receiptService.GetInsightAsync(userId);
                return Ok(new ApiResponse<InsightDto> { Success = true, Data = insight });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get item insight.");
                return StatusCode(500, new ApiResponse<InsightDto> { Success = false, Message = "An error occurred." });
            }
        }

        private string? ValidateUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return "Image file is required.";

            var maxSize = _configuration.GetValue<long?>("Uploads:MaxFileSizeBytes") ?? 8_388_608;
            if (file.Length > maxSize)
                return "Image file must be 8 MB or smaller.";

            var allowedExtensions = _configuration.GetSection("Uploads:AllowedExtensions").Get<string[]>()
                ?? new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            return allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
                ? null
                : "Only JPG, PNG, and WebP images are allowed.";
        }

        private static List<string> ValidateReceiptRequest(string storeName, decimal totalAmount, DateTime date)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(storeName))
                errors.Add("Store name is required.");
            if (totalAmount <= 0)
                errors.Add("Enter a valid total amount.");
            if (date == default)
                errors.Add("Pick a valid date.");

            return errors;
        }

        private static ReceiptItem MapItemDtoToEntity(ReceiptItemDto itemDto)
        {
            return new ReceiptItem
            {
                ItemName = itemDto.ItemName.Trim(),
                Price = itemDto.Price,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                Barcode = itemDto.Barcode,
                Unit = itemDto.Unit
            };
        }

        private static ReceiptDto MapToDto(Receipt receipt)
        {
            return new ReceiptDto
            {
                Id = receipt.Id,
                StoreName = receipt.StoreName,
                Date = receipt.Date,
                TotalAmount = receipt.TotalAmount,
                PhotoUrl = receipt.PhotoUrl,
                CreatedAt = receipt.CreatedAt,
                CategoryId = receipt.CategoryId,
                CategoryName = receipt.Category?.Name,
                StoreId = receipt.StoreId,
                Store = receipt.Store != null ? new StoreDto
                {
                    Id = receipt.Store.Id,
                    Name = receipt.Store.Name,
                    Address = receipt.Store.Address,
                    Phone = receipt.Store.Phone,
                    TaxNumber = receipt.Store.TaxNumber
                } : null,
                Items = receipt.Items.Select(item => new ReceiptItemDto
                {
                    Id = item.Id,
                    ItemName = item.ItemName,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Barcode = item.Barcode,
                    Unit = item.Unit
                }).ToList()
            };
        }
    }
}
