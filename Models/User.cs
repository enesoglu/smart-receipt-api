namespace smart_receipt_api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public List<Receipt> Receipts { get; set; } = new List<Receipt>();
    }
}
