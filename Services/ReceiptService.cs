using Microsoft.EntityFrameworkCore;
using smart_receipt_api.DTOs;
using smart_receipt_api.Models;
using smart_receipt_api.Repositories;
using System.Globalization;
using System.Text.RegularExpressions;

namespace smart_receipt_api.Services
{
    public class ReceiptService : IReceiptService
    {
        private readonly IRepository<Receipt> _receiptRepository;
        private readonly AppDbContext _context;

        public ReceiptService(IRepository<Receipt> receiptRepository, AppDbContext context)
        {
            _receiptRepository = receiptRepository;
            _context = context;
        }

        public async Task<IEnumerable<Receipt>> GetUserReceiptsAsync(int userId)
        {
            return await BaseReceiptQuery()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.Id)
                .ToListAsync();
        }

        public Task<Receipt?> GetReceiptByIdAsync(int id)
        {
            return BaseReceiptQuery().FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Receipt> CreateReceiptAsync(Receipt receipt)
        {
            await _receiptRepository.AddAsync(receipt);
            await _receiptRepository.SaveAsync();
            return receipt;
        }

        public async Task<Receipt> CreateReceiptWithStoreAsync(CreateReceiptRequest request, int userId)
        {
            var store = await GetOrCreateStoreAsync(request.StoreName);
            var categoryId = await ResolveVisibleCategoryIdAsync(request.CategoryId, userId);

            var receipt = new Receipt
            {
                UserId = userId,
                StoreName = request.StoreName.Trim(),
                Date = request.Date,
                TotalAmount = request.TotalAmount,
                PhotoUrl = request.PhotoUrl,
                CategoryId = categoryId,
                StoreId = request.StoreId ?? store?.Id,
                CreatedAt = DateTime.UtcNow,
                Items = request.Items.Select(MapItemDtoToEntity).ToList()
            };

            await _receiptRepository.AddAsync(receipt);
            await _receiptRepository.SaveAsync();
            return (await GetReceiptByIdAsync(receipt.Id)) ?? receipt;
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
            var inclusiveEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            return await BaseReceiptQuery()
                .Where(r => r.UserId == userId && r.Date >= startDate.Date && r.Date <= inclusiveEndDate)
                .OrderByDescending(r => r.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<Receipt>> FilterReceiptsByStoreAsync(int userId, string storeName)
        {
            if (string.IsNullOrWhiteSpace(storeName))
                return await GetUserReceiptsAsync(userId);

            var normalizedStoreName = storeName.Trim().ToLowerInvariant();

            return await BaseReceiptQuery()
                .Where(r => r.UserId == userId && r.StoreName.ToLower().Contains(normalizedStoreName))
                .OrderByDescending(r => r.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<Receipt>> SearchReceiptsAsync(int userId, string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await GetUserReceiptsAsync(userId);

            var normalizedQuery = query.Trim().ToLowerInvariant();

            return await BaseReceiptQuery()
                .Where(r => r.UserId == userId
                    && (r.StoreName.ToLower().Contains(normalizedQuery)
                        || r.Items.Any(i => i.ItemName.ToLower().Contains(normalizedQuery))))
                .OrderByDescending(r => r.Date)
                .ToListAsync();
        }

        public async Task<List<ItemAggregateDto>> GetTopItemsAsync(int userId, int limit, int? year, int? month)
        {
            var clampedLimit = Math.Clamp(limit, 1, 50);
            var query = BaseReceiptQuery().Where(r => r.UserId == userId);

            if (year.HasValue && month.HasValue && month.Value is >= 1 and <= 12)
            {
                var startDate = new DateTime(year.Value, month.Value, 1);
                var endDate = startDate.AddMonths(1).AddTicks(-1);
                query = query.Where(r => r.Date >= startDate && r.Date <= endDate);
            }

            var receipts = await query.ToListAsync();

            return receipts
                .SelectMany(r => r.Items)
                .Where(i => !string.IsNullOrWhiteSpace(i.ItemName))
                .GroupBy(i => i.ItemName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new ItemAggregateDto
                {
                    ItemName = g.Key,
                    TotalSpent = g.Sum(i => i.Price),
                    OccurrenceCount = g.Count(),
                    AverageUnitPrice = Math.Round(g.Average(i => i.UnitPrice > 0 ? i.UnitPrice : i.Price), 2)
                })
                .OrderByDescending(i => i.TotalSpent)
                .ThenBy(i => i.ItemName)
                .Take(clampedLimit)
                .ToList();
        }

        public async Task<InsightDto> GetInsightAsync(int userId)
        {
            var startDate = DateTime.UtcNow.AddDays(-30);
            var receipts = await BaseReceiptQuery()
                .Where(r => r.UserId == userId && r.Date >= startDate)
                .ToListAsync();

            var topItem = receipts
                .SelectMany(r => r.Items)
                .Where(i => !string.IsNullOrWhiteSpace(i.ItemName))
                .GroupBy(i => i.ItemName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new { ItemName = g.Key, TotalSpent = g.Sum(i => i.Price) })
                .OrderByDescending(i => i.TotalSpent)
                .FirstOrDefault();

            if (topItem == null)
                return new InsightDto { Message = "No spending recorded in the last 30 days yet." };

            return new InsightDto
            {
                Message = $"In the last 30 days, you spent the most on {topItem.ItemName} ({FormatTry(topItem.TotalSpent)})."
            };
        }

        /// <summary>
        /// Builds a scan result from raw OCR text when Document Intelligence cannot identify a receipt document.
        /// </summary>
        public ScanResultDto BuildScanResult(string rawText)
        {
            var items = ParseReceiptItems(rawText)
                .Select(MapItemEntityToDto)
                .ToList();

            return new ScanResultDto
            {
                RawText = rawText,
                StoreName = GuessStoreName(rawText),
                Date = GuessDate(rawText),
                TotalAmount = GuessTotalAmount(rawText),
                Items = items
            };
        }

        public List<ReceiptItem> ParseReceiptItems(string ocrText)
        {
            var items = new List<ReceiptItem>();
            if (string.IsNullOrWhiteSpace(ocrText))
                return items;

            var lines = ocrText
                .Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            var itemPattern = new Regex(@"^(\d+)?\s*(\d+(?:[,.]\d+)?)\s*(AD|PK|KG)\s*[\u00D7xX]\s*(\d+(?:[,.]\d+)?)", RegexOptions.IgnoreCase);
            var pricePattern = new Regex(@"\*\s*(\d+(?:[,.]\d+)?)");

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var match = itemPattern.Match(line);

                if (!match.Success)
                    continue;

                var barcode = match.Groups[1].Success ? match.Groups[1].Value : null;
                var quantity = ParseAmount(match.Groups[2].Value) ?? 1m;
                var unit = match.Groups[3].Value.ToUpperInvariant();
                var unitPrice = ParseAmount(match.Groups[4].Value) ?? 0m;
                var itemName = string.Empty;

                if (i + 1 < lines.Count)
                {
                    var nextLine = lines[i + 1];
                    if (!Regex.IsMatch(nextLine, @"^[\d\*]") && !ContainsAny(nextLine, "TOPLAM", "TOTAL", "KDV"))
                    {
                        itemName = nextLine.Split('%')[0].Trim();
                        i++;
                    }
                }

                if (string.IsNullOrWhiteSpace(itemName))
                {
                    var afterMatch = line[(match.Index + match.Length)..];
                    itemName = afterMatch.Split('%')[0].Trim();
                }

                var priceMatch = pricePattern.Match(line);
                if (!priceMatch.Success && i + 1 < lines.Count)
                    priceMatch = pricePattern.Match(lines[i + 1]);

                var totalPrice = priceMatch.Success
                    ? ParseAmount(priceMatch.Groups[1].Value) ?? 0m
                    : quantity * unitPrice;

                itemName = Regex.Replace(itemName, @"-[A-Z]$", string.Empty).Trim();

                if (!string.IsNullOrWhiteSpace(itemName) && totalPrice > 0)
                {
                    items.Add(new ReceiptItem
                    {
                        ItemName = itemName,
                        Price = totalPrice,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        Barcode = barcode,
                        Unit = unit == "PK" ? "AD" : unit
                    });
                }
            }

            return items;
        }

        private IQueryable<Receipt> BaseReceiptQuery()
        {
            return _context.Receipts
                .Include(r => r.Items)
                .Include(r => r.Category)
                .Include(r => r.Store);
        }

        private async Task<Store?> GetOrCreateStoreAsync(string storeName)
        {
            if (string.IsNullOrWhiteSpace(storeName))
                return null;

            var trimmedStoreName = storeName.Trim();
            var normalizedStoreName = trimmedStoreName.ToLowerInvariant();

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.Name.ToLower() == normalizedStoreName);

            if (store != null)
                return store;

            store = new Store { Name = trimmedStoreName };
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();
            return store;
        }

        private async Task<int?> ResolveVisibleCategoryIdAsync(int? categoryId, int userId)
        {
            if (categoryId == null)
                return null;

            var isVisible = await _context.Categories
                .AnyAsync(c => c.Id == categoryId && (c.UserId == null || c.UserId == userId));

            return isVisible ? categoryId : null;
        }

        private static ReceiptItem MapItemDtoToEntity(ReceiptItemDto item)
        {
            return new ReceiptItem
            {
                ItemName = item.ItemName.Trim(),
                Price = item.Price,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Barcode = string.IsNullOrWhiteSpace(item.Barcode) ? null : item.Barcode.Trim(),
                Unit = string.IsNullOrWhiteSpace(item.Unit) ? null : item.Unit.Trim()
            };
        }

        private static ReceiptItemDto MapItemEntityToDto(ReceiptItem item)
        {
            return new ReceiptItemDto
            {
                Id = item.Id,
                ItemName = item.ItemName,
                Price = item.Price,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Barcode = item.Barcode,
                Unit = item.Unit
            };
        }

        private static string? GuessStoreName(string rawText)
        {
            var candidates = rawText
                .Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 3
                    && Regex.IsMatch(l, "[A-Za-z]")
                    && !Regex.IsMatch(l, @"^\d")
                    && !ContainsAny(l, "TOPLAM", "TOTAL", "TUTAR", "FIS", "FATURA"))
                .Take(3)
                .ToList();

            return candidates
                .OrderByDescending(c => c.Count(char.IsUpper))
                .ThenByDescending(c => c.Length)
                .FirstOrDefault();
        }

        private static DateTime? GuessDate(string rawText)
        {
            var candidates = new List<DateTime>();
            var now = DateTime.UtcNow.Date;

            foreach (Match match in Regex.Matches(rawText, @"(\d{1,2})[./-](\d{1,2})[./-](\d{2,4})"))
            {
                var day = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                var month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                var year = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                if (year < 100)
                    year += 2000;

                if (TryCreateDate(year, month, day, out var date) && date.Date <= now)
                    candidates.Add(date);
            }

            foreach (Match match in Regex.Matches(rawText, @"(\d{4})-(\d{2})-(\d{2})"))
            {
                var year = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                var month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                var day = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

                if (TryCreateDate(year, month, day, out var date) && date.Date <= now)
                    candidates.Add(date);
            }

            var best = candidates
                .OrderByDescending(d => d)
                .FirstOrDefault();

            return best == default ? null : best;
        }

        private static decimal? GuessTotalAmount(string rawText)
        {
            foreach (var line in rawText.Split('\n').Select(l => l.Trim()))
            {
                if (!ContainsAny(line, "TOPLAM", "TOTAL", "TUTAR"))
                    continue;

                var amount = Regex.Matches(line, @"\d+(?:[.,]\d{2})?")
                    .Select(m => ParseAmount(m.Value))
                    .Where(v => v.HasValue)
                    .Select(v => v!.Value)
                    .DefaultIfEmpty()
                    .Max();

                if (amount > 0)
                    return amount;
            }

            var maxAmount = Regex.Matches(rawText, @"\d+(?:[.,]\d{2})")
                .Select(m => ParseAmount(m.Value))
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .DefaultIfEmpty()
                .Max();

            return maxAmount > 0 ? maxAmount : null;
        }

        private static bool TryCreateDate(int year, int month, int day, out DateTime date)
        {
            date = default;
            if (year < 2000 || month is < 1 or > 12 || day is < 1 or > 31)
                return false;

            try
            {
                date = new DateTime(year, month, day);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static decimal? ParseAmount(string value)
        {
            var normalized = value.Trim();
            if (normalized.Contains('.') && normalized.Contains(','))
            {
                normalized = normalized.LastIndexOf(",", StringComparison.Ordinal) > normalized.LastIndexOf(".", StringComparison.Ordinal)
                    ? normalized.Replace(".", string.Empty).Replace(',', '.')
                    : normalized.Replace(",", string.Empty);
            }
            else
            {
                normalized = normalized.Replace(',', '.');
            }

            return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount)
                ? amount
                : null;
        }

        private static bool ContainsAny(string value, params string[] terms)
        {
            return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        private static string FormatTry(decimal amount)
        {
            return $"\u20BA {amount:0.00}";
        }
    }
}
