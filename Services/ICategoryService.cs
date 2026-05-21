using smart_receipt_api.DTOs;
using smart_receipt_api.Models;

namespace smart_receipt_api.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetVisibleAsync(int userId);
        Task<Category?> GetByIdAsync(int id, int userId);
        Task<Category> CreateAsync(int userId, UpsertCategoryRequest req);
        Task<Category?> UpdateAsync(int id, int userId, UpsertCategoryRequest req);
        Task<bool> DeleteAsync(int id, int userId);
        Task<Category?> SetBudgetAsync(int id, int userId, decimal? limit);
    }
}
