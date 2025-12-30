namespace smart_receipt_api.Models
{
    public class Receipt
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ImagePath { get; set; }
        public string? Tags { get; set; }  // CSV format: "Groceries,Organic,Weekly"

        public int UserId { get; set; }
        public User? User { get; set; }

        public List<ReceiptItems> Items { get; set; } = new List<ReceiptItems>();
    }
}
