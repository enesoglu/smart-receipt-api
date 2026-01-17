namespace smart_receipt_api.Models
{
    public class Receipt
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ImagePath { get; set; }

        // User relationship
        public int UserId { get; set; }
        public User? User { get; set; }

        // Category relationship (optional)
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        // Store relationship (optional)
        public int? StoreId { get; set; }
        public Store? Store { get; set; }

        public List<ReceiptItems> Items { get; set; } = new List<ReceiptItems>();
    }
}
