namespace smart_receipt_api.Services
{
    public interface IOcrService
    {
        Task<Dictionary<string, string>> ExtractReceiptDataAsync(IFormFile imageFile);
    }
}

