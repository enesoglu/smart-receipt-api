namespace smart_receipt_api.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public decimal? MonthlyBudgetLimit { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? UserId { get; set; }
        public User? User { get; set; }

        public List<Receipt> Receipts { get; set; } = new();
    }
}
