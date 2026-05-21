namespace smart_receipt_api.Models
{
    public class Receipt
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        public User? User { get; set; }

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public int? StoreId { get; set; }
        public Store? Store { get; set; }

        public List<ReceiptItem> Items { get; set; } = new();
    }
}
