namespace smart_receipt_api.Models
{
    public class ReceiptItems
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;   // product name
        public decimal Price { get; set; }                        // product price      


        // which receipt does this item belong to?
        public int ReceiptId { get; set; }
        public Receipt? Receipt { get; set; }
    }
}
