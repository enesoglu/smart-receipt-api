namespace smart_receipt_api.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
    }

    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public decimal? MonthlyBudgetLimit { get; set; }
        public bool IsSystemDefault { get; set; }
    }

    public class UpsertCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public decimal? MonthlyBudgetLimit { get; set; }
    }

    public class SetBudgetRequest
    {
        public decimal? MonthlyBudgetLimit { get; set; }
    }

    public class BudgetStatusDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public decimal MonthlyBudgetLimit { get; set; }
        public decimal Spent { get; set; }
        public decimal Remaining { get; set; }
        public decimal UsagePercent { get; set; }
        public bool IsOverBudget { get; set; }
        public bool HasBudget { get; set; }
    }

    public class BudgetSummaryDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal TotalRemaining { get; set; }
        public int OverBudgetCategoryCount { get; set; }
        public List<BudgetStatusDto> Categories { get; set; } = new();
    }

    public class StoreDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? TaxNumber { get; set; }
    }

    public class CreateStoreRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? TaxNumber { get; set; }
    }

    public class CreateReceiptRequest
    {
        public string StoreName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public string? PhotoUrl { get; set; }
        public int? CategoryId { get; set; }
        public int? StoreId { get; set; }
        public List<ReceiptItemDto> Items { get; set; } = new();
    }

    public class UpdateReceiptRequest
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public string? PhotoUrl { get; set; }
        public int? CategoryId { get; set; }
        public int? StoreId { get; set; }
        public List<ReceiptItemDto> Items { get; set; } = new();
    }

    public class ReceiptDto
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? StoreId { get; set; }
        public StoreDto? Store { get; set; }
        public List<ReceiptItemDto> Items { get; set; } = new();
    }

    public class ReceiptItemDto
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public string? Barcode { get; set; }
        public string? Unit { get; set; }
    }

    public class ReceiptPhotoDto
    {
        public string PhotoUrl { get; set; } = string.Empty;
    }

    public class ScanResultDto
    {
        public string RawText { get; set; } = string.Empty;
        public string? StoreName { get; set; }
        public DateTime? Date { get; set; }
        public decimal? TotalAmount { get; set; }
        public List<ReceiptItemDto> Items { get; set; } = new();
    }

    public class ItemAggregateDto
    {
        public string ItemName { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        public int OccurrenceCount { get; set; }
        public decimal AverageUnitPrice { get; set; }
    }

    public class InsightDto
    {
        public string Message { get; set; } = string.Empty;
    }

    public class DashboardStatsDto
    {
        public decimal TotalMonthlySpending { get; set; }
        public decimal AverageReceiptValue { get; set; }
        public string MostFrequentStore { get; set; } = string.Empty;
        public int MostFrequentStoreVisitCount { get; set; }
    }

    public class DailySpendingDto
    {
        public string Date { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class StoreSpendingDto
    {
        public string StoreName { get; set; } = string.Empty;
        public decimal TotalSpending { get; set; }
        public int ReceiptCount { get; set; }
    }

    public class MonthlySpendingDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalSpending { get; set; }
    }

    public class CategorySpendingDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalSpending { get; set; }
        public int ReceiptCount { get; set; }
    }

    public class DashboardDto
    {
        public decimal TotalMonthlySpending { get; set; }
        public List<MonthlySpendingDto> MonthlyData { get; set; } = new();
        public List<StoreSpendingDto> StoreData { get; set; } = new();
        public List<CategorySpendingDto> CategoryData { get; set; } = new();
    }
}
