namespace smart_receipt_api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Receipt> Receipts { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }
}
