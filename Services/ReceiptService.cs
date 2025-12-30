using smart_receipt_api.Models;
using smart_receipt_api.Repositories;

namespace smart_receipt_api.Services
{
    public class ReceiptService : IReceiptService
    {
        private readonly IRepository<Receipt> _receiptRepository;

        public ReceiptService(IRepository<Receipt> receiptRepository)
        {
            _receiptRepository = receiptRepository;
        }

        public async Task<IEnumerable<Receipt>> GetUserReceiptsAsync(int userId)
        {
            var receipts = await _receiptRepository.GetAllAsync();
            return receipts.Where(r => r.UserId == userId).ToList();
        }

        public async Task<Receipt> GetReceiptByIdAsync(int id)
        {
            return await _receiptRepository.GetByIdAsync(id);
        }

        public async Task<Receipt> CreateReceiptAsync(Receipt receipt)
        {
            await _receiptRepository.AddAsync(receipt);
            await _receiptRepository.SaveAsync();
            return receipt;
        }

        public async Task<Receipt> UpdateReceiptAsync(Receipt receipt)
        {
            await _receiptRepository.UpdateAsync(receipt);
            return receipt;
        }

        public async Task DeleteReceiptAsync(int id)
        {
            await _receiptRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<Receipt>> FilterReceiptsByDateAsync(int userId, DateTime startDate, DateTime endDate)
        {
            var receipts = await GetUserReceiptsAsync(userId);
            return receipts.Where(r => r.Date >= startDate && r.Date <= endDate).ToList();
        }

        public async Task<IEnumerable<Receipt>> FilterReceiptsByStoreAsync(int userId, string storeName)
        {
            var receipts = await GetUserReceiptsAsync(userId);
            return receipts.Where(r => r.StoreName.Contains(storeName, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}

