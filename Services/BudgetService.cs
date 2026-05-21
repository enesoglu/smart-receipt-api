using Microsoft.EntityFrameworkCore;
using smart_receipt_api.DTOs;

namespace smart_receipt_api.Services
{
    public class BudgetService : IBudgetService
    {
        private readonly AppDbContext _context;

        public BudgetService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<BudgetStatusDto>> GetStatusAsync(int userId, int? year, int? month)
        {
            var (targetYear, targetMonth, startDate, endDate) = ResolveMonth(year, month);

            var categories = await _context.Categories
                .Where(c => c.UserId == null || c.UserId == userId)
                .ToListAsync();

            var spendingByCategory = await _context.Receipts
                .Where(r => r.UserId == userId
                    && r.CategoryId != null
                    && r.Date >= startDate
                    && r.Date <= endDate)
                .GroupBy(r => r.CategoryId!.Value)
                .Select(g => new { CategoryId = g.Key, Spent = g.Sum(r => r.TotalAmount) })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Spent);

            return categories
                .Select(category =>
                {
                    var spent = spendingByCategory.TryGetValue(category.Id, out var value) ? value : 0m;
                    var limit = category.MonthlyBudgetLimit ?? 0m;
                    var hasBudget = limit > 0m;

                    return new BudgetStatusDto
                    {
                        CategoryId = category.Id,
                        CategoryName = category.Name,
                        IconUrl = category.IconUrl,
                        MonthlyBudgetLimit = limit,
                        Spent = spent,
                        Remaining = limit - spent,
                        UsagePercent = hasBudget ? Math.Round(spent / limit * 100m, 2) : 0m,
                        IsOverBudget = hasBudget && spent > limit,
                        HasBudget = hasBudget
                    };
                })
                .OrderByDescending(b => b.HasBudget)
                .ThenByDescending(b => b.Spent)
                .ThenBy(b => b.CategoryName)
                .ToList();
        }

        public async Task<List<BudgetStatusDto>> GetAlertsAsync(int userId, int? year, int? month)
        {
            var statuses = await GetStatusAsync(userId, year, month);
            return statuses.Where(s => s.IsOverBudget).ToList();
        }

        public async Task<BudgetSummaryDto> GetSummaryAsync(int userId, int? year, int? month)
        {
            var (targetYear, targetMonth, _, _) = ResolveMonth(year, month);
            var categories = await GetStatusAsync(userId, targetYear, targetMonth);

            return new BudgetSummaryDto
            {
                Year = targetYear,
                Month = targetMonth,
                TotalBudget = categories.Where(c => c.HasBudget).Sum(c => c.MonthlyBudgetLimit),
                TotalSpent = categories.Sum(c => c.Spent),
                TotalRemaining = categories.Where(c => c.HasBudget).Sum(c => c.Remaining),
                OverBudgetCategoryCount = categories.Count(c => c.IsOverBudget),
                Categories = categories
            };
        }

        private static (int Year, int Month, DateTime StartDate, DateTime EndDate) ResolveMonth(int? year, int? month)
        {
            var now = DateTime.UtcNow;
            var targetYear = year ?? now.Year;
            var targetMonth = month ?? now.Month;

            if (targetMonth < 1 || targetMonth > 12)
                targetMonth = now.Month;

            var startDate = new DateTime(targetYear, targetMonth, 1);
            var endDate = startDate.AddMonths(1).AddTicks(-1);
            return (targetYear, targetMonth, startDate, endDate);
        }
    }
}
