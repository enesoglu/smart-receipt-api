using smart_receipt_api.DTOs;

namespace smart_receipt_api.Services
{
    public interface IOcrService
    {
        Task<ScanResultDto> ScanReceiptAsync(IFormFile imageFile);
    }
}

