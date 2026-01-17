namespace smart_receipt_api.DTOs
{
    // ===== API Response Wrapper =====
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
    }

    // ===== User DTOs =====
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    // ===== Category DTOs =====
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconName { get; set; }
    }

    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconName { get; set; }
    }

    // ===== Store DTOs =====
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

    // ===== Receipt DTOs =====
    public class CreateReceiptRequest
    {
        public string StoreName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ImagePath { get; set; }
        public int? CategoryId { get; set; }
        public int? StoreId { get; set; }
        public List<ReceiptItemDto> Items { get; set; } = new List<ReceiptItemDto>();
    }

    public class UpdateReceiptRequest
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ImagePath { get; set; }
        public int? CategoryId { get; set; }
        public int? StoreId { get; set; }
        public List<ReceiptItemDto> Items { get; set; } = new List<ReceiptItemDto>();
    }

    public class ReceiptDto
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ImagePath { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? StoreId { get; set; }
        public StoreDto? Store { get; set; }
        public List<ReceiptItemDto> Items { get; set; } = new List<ReceiptItemDto>();
    }

    public class ReceiptItemDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public string? Barcode { get; set; }
        public string? Unit { get; set; }
    }

    // ===== Dashboard & Statistics DTOs =====
    public class DashboardStatsDto
    {
        public decimal TotalMonthlySpending { get; set; }
        public decimal AverageReceiptValue { get; set; }
        public string MostFrequentStore { get; set; } = string.Empty;
        public int MostFrequentStoreVisitCount { get; set; }
    }

    public class DailySpendingDto
    {
        public string Date { get; set; } = string.Empty;  // Format: yyyy-MM-dd
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
        public List<MonthlySpendingDto> MonthlyData { get; set; } = new List<MonthlySpendingDto>();
        public List<StoreSpendingDto> StoreData { get; set; } = new List<StoreSpendingDto>();
        public List<CategorySpendingDto> CategoryData { get; set; } = new List<CategorySpendingDto>();
    }
}

