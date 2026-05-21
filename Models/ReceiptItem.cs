namespace smart_receipt_api.Models
{
    public class ReceiptItem
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public string? Barcode { get; set; }
        public string? Unit { get; set; }

        public int ReceiptId { get; set; }
        public Receipt? Receipt { get; set; }
    }
}
