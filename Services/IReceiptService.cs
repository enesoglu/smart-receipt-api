using smart_receipt_api.DTOs;
using smart_receipt_api.Models;

namespace smart_receipt_api.Services
{
    public interface IReceiptService
    {
        Task<IEnumerable<Receipt>> GetUserReceiptsAsync(int userId);
        Task<Receipt> GetReceiptByIdAsync(int id);
        Task<Receipt> CreateReceiptAsync(Receipt receipt);
        Task<Receipt> CreateReceiptWithStoreAsync(CreateReceiptRequest request, int userId);
        Task<Receipt> UpdateReceiptAsync(Receipt receipt);
        Task DeleteReceiptAsync(int id);
        Task<IEnumerable<Receipt>> FilterReceiptsByDateAsync(int userId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<Receipt>> FilterReceiptsByStoreAsync(int userId, string storeName);
        List<ReceiptItems> ParseReceiptItems(string ocrText);
    }
}

