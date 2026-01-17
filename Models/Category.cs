namespace smart_receipt_api.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconName { get; set; }  // For UI icon display

        // Navigation property
        public List<Receipt> Receipts { get; set; } = new List<Receipt>();
    }
}
