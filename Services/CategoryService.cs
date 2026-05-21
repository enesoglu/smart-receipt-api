using Microsoft.EntityFrameworkCore;
using smart_receipt_api.DTOs;
using smart_receipt_api.Models;

namespace smart_receipt_api.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetVisibleAsync(int userId)
        {
            return await _context.Categories
                .Where(c => c.UserId == null || c.UserId == userId)
                .OrderBy(c => c.UserId == null ? 0 : 1)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public Task<Category?> GetByIdAsync(int id, int userId)
        {
            return _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && (c.UserId == null || c.UserId == userId));
        }

        public async Task<Category> CreateAsync(int userId, UpsertCategoryRequest req)
        {
            var category = new Category
            {
                UserId = userId,
                Name = req.Name.Trim(),
                IconUrl = string.IsNullOrWhiteSpace(req.IconUrl) ? null : req.IconUrl.Trim(),
                MonthlyBudgetLimit = req.MonthlyBudgetLimit,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category?> UpdateAsync(int id, int userId, UpsertCategoryRequest req)
        {
            var category = await GetByIdAsync(id, userId);
            if (category == null || category.UserId == null)
                return null;

            category.Name = req.Name.Trim();
            category.IconUrl = string.IsNullOrWhiteSpace(req.IconUrl) ? null : req.IconUrl.Trim();
            category.MonthlyBudgetLimit = req.MonthlyBudgetLimit;

            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var category = await GetByIdAsync(id, userId);
            if (category == null || category.UserId == null)
                return false;

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Category?> SetBudgetAsync(int id, int userId, decimal? limit)
        {
            var category = await GetByIdAsync(id, userId);
            if (category == null || category.UserId == null)
                return null;

            category.MonthlyBudgetLimit = limit;
            await _context.SaveChangesAsync();
            return category;
        }
    }
}
