namespace smart_receipt_api.Models
{
    public class Receipt
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = string.Empty;   // store name
        public DateTime Date { get; set; }                      // receipt date
        public decimal TotalAmount { get; set; }                // receipt amount
        public string? ImagePath { get; set; }                  // receipt's image path


        // whose receipt is this?
        public int UserId { get; set; }
        public User? User { get; set; }

        // items in the receipt 
        // public List<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();
    }
}
