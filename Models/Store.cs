namespace smart_receipt_api.Models
{
    public class Store
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? TaxNumber { get; set; }

        // Navigation property
        public List<Receipt> Receipts { get; set; } = new List<Receipt>();
    }
}

