using smart_receipt_api.DTOs;

namespace smart_receipt_api.Services
{
    public interface IBudgetService
    {
        Task<List<BudgetStatusDto>> GetStatusAsync(int userId, int? year, int? month);
        Task<List<BudgetStatusDto>> GetAlertsAsync(int userId, int? year, int? month);
        Task<BudgetSummaryDto> GetSummaryAsync(int userId, int? year, int? month);
    }
}
