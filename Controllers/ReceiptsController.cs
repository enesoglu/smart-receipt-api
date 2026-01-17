﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using smart_receipt_api.DTOs;
using smart_receipt_api.Models;
using smart_receipt_api.Services;
using System.Security.Claims;

namespace smart_receipt_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReceiptsController : ControllerBase
    {
        private readonly IReceiptService _receiptService;
        private readonly IOcrService _ocrService;
        private readonly ILogger<ReceiptsController> _logger;

        public ReceiptsController(IReceiptService receiptService, IOcrService ocrService, ILogger<ReceiptsController> logger)
        {
            _receiptService = receiptService;
            _ocrService = ocrService;
            _logger = logger;
        }

        // ===== BASIC CRUD OPERATIONS =====

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ReceiptDto>>>> GetUserReceipts()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "Unauthorized" });

                var receipts = await _receiptService.GetUserReceiptsAsync(userId);
                var dtos = receipts.Select(MapToDto).ToList();
                return Ok(new ApiResponse<List<ReceiptDto>> { Success = true, Data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get receipts error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ReceiptDto>>> GetReceipt(int id)
        {
            try
            {
                var receipt = await _receiptService.GetReceiptByIdAsync(id);
                if (receipt == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Receipt not found" });

                var userId = GetUserId();
                if (receipt.UserId != userId)
                    return Forbid();

                return Ok(new ApiResponse<ReceiptDto> { Success = true, Data = MapToDto(receipt) });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get receipt error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ReceiptDto>>> CreateReceipt(CreateReceiptRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "Unauthorized" });

                var createdReceipt = await _receiptService.CreateReceiptWithStoreAsync(request, userId);
                return CreatedAtAction(nameof(GetReceipt), new { id = createdReceipt.Id },
                    new ApiResponse<ReceiptDto> { Success = true, Data = MapToDto(createdReceipt) });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Create receipt error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<ReceiptDto>>> UpdateReceipt(int id, UpdateReceiptRequest request)
        {
            try
            {
                var userId = GetUserId();
                var existingReceipt = await _receiptService.GetReceiptByIdAsync(id);

                if (existingReceipt == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Receipt not found" });

                if (existingReceipt.UserId != userId)
                    return Forbid();

                // Update receipt fields
                existingReceipt.StoreName = request.StoreName;
                existingReceipt.Date = request.Date;
                existingReceipt.TotalAmount = request.TotalAmount;
                existingReceipt.ImagePath = request.ImagePath;
                existingReceipt.CategoryId = request.CategoryId;
                existingReceipt.StoreId = request.StoreId;

                // Remove old items
                existingReceipt.Items.Clear();

                // Add new items
                foreach (var itemDto in request.Items)
                {
                    existingReceipt.Items.Add(new ReceiptItems
                    {
                        ProductName = itemDto.ProductName,
                        Price = itemDto.Price
                    });
                }

                var updatedReceipt = await _receiptService.UpdateReceiptAsync(existingReceipt);
                return Ok(new ApiResponse<ReceiptDto> { Success = true, Data = MapToDto(updatedReceipt) });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update receipt error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReceipt(int id)
        {
            try
            {
                var userId = GetUserId();
                var receipt = await _receiptService.GetReceiptByIdAsync(id);

                if (receipt == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Receipt not found" });

                if (receipt.UserId != userId)
                    return Forbid();

                await _receiptService.DeleteReceiptAsync(id);
                return Ok(new ApiResponse<object> { Success = true, Message = "Receipt deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete receipt error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        // ===== FILTERING & SEARCH =====

        [HttpGet("filter/store")]
        public async Task<ActionResult<ApiResponse<List<ReceiptDto>>>> FilterByStore([FromQuery] string storeName)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "Unauthorized" });

                var receipts = await _receiptService.FilterReceiptsByStoreAsync(userId, storeName);
                var dtos = receipts.Select(MapToDto).ToList();
                return Ok(new ApiResponse<List<ReceiptDto>> { Success = true, Data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Filter by store error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpGet("filter/date")]
        public async Task<ActionResult<ApiResponse<List<ReceiptDto>>>> FilterByDate([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "Unauthorized" });

                var receipts = await _receiptService.FilterReceiptsByDateAsync(userId, startDate, endDate);
                var dtos = receipts.Select(MapToDto).ToList();
                return Ok(new ApiResponse<List<ReceiptDto>> { Success = true, Data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Filter by date error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<ReceiptDto>>>> SearchReceipts([FromQuery] string query)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "Unauthorized" });

                if (string.IsNullOrWhiteSpace(query))
                {
                    var allReceipts = await _receiptService.GetUserReceiptsAsync(userId);
                    var allDtos = allReceipts.Select(MapToDto).ToList();
                    return Ok(new ApiResponse<List<ReceiptDto>> { Success = true, Data = allDtos });
                }

                var receipts = await _receiptService.FilterReceiptsByStoreAsync(userId, query);
                var dtos = receipts.Select(MapToDto).ToList();
                return Ok(new ApiResponse<List<ReceiptDto>> { Success = true, Data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Search receipts error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        // ===== STATISTICS & ANALYTICS =====

        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "Unauthorized" });

                var receipts = (await _receiptService.GetUserReceiptsAsync(userId)).ToList();

                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var monthlyReceipts = receipts
                    .Where(r => r.Date.Month == currentMonth && r.Date.Year == currentYear)
                    .ToList();

                var totalMonthlySpending = monthlyReceipts.Sum(r => r.TotalAmount);
                var averageReceiptValue = receipts.Count > 0 ? receipts.Average(r => r.TotalAmount) : 0;
                var storeGroups = receipts.GroupBy(r => r.StoreName).ToList();
                var mostFrequentStore = storeGroups.OrderByDescending(g => g.Count()).FirstOrDefault();

                var stats = new DashboardStatsDto
                {
                    TotalMonthlySpending = totalMonthlySpending,
                    AverageReceiptValue = averageReceiptValue,
                    MostFrequentStore = mostFrequentStore?.Key ?? "N/A",
                    MostFrequentStoreVisitCount = mostFrequentStore?.Count() ?? 0
                };

                return Ok(new ApiResponse<DashboardStatsDto> { Success = true, Data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get stats error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpGet("daily-spending")]
        public async Task<ActionResult<ApiResponse<List<DailySpendingDto>>>> GetDailySpending([FromQuery] int year, [FromQuery] int month)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "Unauthorized" });

                var receipts = (await _receiptService.GetUserReceiptsAsync(userId)).ToList();
                var monthReceipts = receipts
                    .Where(r => r.Date.Year == year && r.Date.Month == month)
                    .ToList();

                // Get all days in the month
                var daysInMonth = DateTime.DaysInMonth(year, month);
                var dailySpending = new List<DailySpendingDto>();

                for (int day = 1; day <= daysInMonth; day++)
                {
                    var dateStr = new DateTime(year, month, day).ToString("yyyy-MM-dd");
                    var dayTotal = monthReceipts
                        .Where(r => r.Date.Date == new DateTime(year, month, day))
                        .Sum(r => r.TotalAmount);

                    dailySpending.Add(new DailySpendingDto
                    {
                        Date = dateStr,
                        Amount = dayTotal
                    });
                }

                return Ok(new ApiResponse<List<DailySpendingDto>> { Success = true, Data = dailySpending });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get daily spending error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpGet("store-stats")]
        public async Task<ActionResult<ApiResponse<List<StoreSpendingDto>>>> GetStoreStats()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "Unauthorized" });

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
                _logger.LogError($"Get store stats error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<DashboardDto>>> GetDashboard()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "Unauthorized" });

                var receipts = (await _receiptService.GetUserReceiptsAsync(userId)).ToList();

                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var monthlyTotal = receipts
                    .Where(r => r.Date.Month == currentMonth && r.Date.Year == currentYear)
                    .Sum(r => r.TotalAmount);

                var monthlyData = receipts
                    .GroupBy(r => new { r.Date.Year, r.Date.Month })
                    .Select(g => new MonthlySpendingDto
                    {
                        Month = g.Key.Month,
                        Year = g.Key.Year,
                        TotalSpending = g.Sum(r => r.TotalAmount)
                    })
                    .OrderBy(m => m.Year)
                    .ThenBy(m => m.Month)
                    .ToList();

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

                var dashboard = new DashboardDto
                {
                    TotalMonthlySpending = monthlyTotal,
                    MonthlyData = monthlyData,
                    StoreData = storeData
                };

                return Ok(new ApiResponse<DashboardDto> { Success = true, Data = dashboard });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get dashboard error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        // ===== OCR & IMAGE UPLOAD =====

        [HttpPost("scan")]
        public async Task<ActionResult<ApiResponse<Dictionary<string, object>>>> ScanReceipt(IFormFile image)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "Unauthorized" });

                if (image == null || image.Length == 0)
                    return BadRequest(new ApiResponse<object> 
                    { 
                        Success = false, 
                        Message = "Image file is required",
                        Errors = new List<string> { "No image file provided" }
                    });

                var extractedData = await _ocrService.ExtractReceiptDataAsync(image);

                // Parse items from OCR text
                var result = new Dictionary<string, object>();
                foreach (var kvp in extractedData)
                {
                    result[kvp.Key] = kvp.Value;
                }

                if (extractedData.TryGetValue("rawText", out var rawText))
                {
                    var parsedItems = _receiptService.ParseReceiptItems(rawText);
                    result["items"] = parsedItems.Select(item => new ReceiptItemDto
                    {
                        ProductName = item.ProductName,
                        Price = item.Price,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Barcode = item.Barcode,
                        Unit = item.Unit
                    }).ToList();
                }

                return Ok(new ApiResponse<Dictionary<string, object>> { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Scan receipt error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "An error occurred during OCR processing" 
                });
            }
        }

        // ===== HELPER METHODS =====

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
        }

        private ReceiptDto MapToDto(Receipt receipt)
        {
            return new ReceiptDto
            {
                Id = receipt.Id,
                StoreName = receipt.StoreName,
                Date = receipt.Date,
                TotalAmount = receipt.TotalAmount,
                ImagePath = receipt.ImagePath,
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
                    ProductName = item.ProductName,
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

