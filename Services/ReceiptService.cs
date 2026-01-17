using Microsoft.EntityFrameworkCore;
using smart_receipt_api.DTOs;
using smart_receipt_api.Models;
using smart_receipt_api.Repositories;
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
            return await _context.Receipts
                .Include(r => r.Items)
                .Include(r => r.Category)
                .Include(r => r.Store)
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        public async Task<Receipt> GetReceiptByIdAsync(int id)
        {
            return await _context.Receipts
                .Include(r => r.Items)
                .Include(r => r.Category)
                .Include(r => r.Store)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Receipt> CreateReceiptAsync(Receipt receipt)
        {
            await _receiptRepository.AddAsync(receipt);
            await _receiptRepository.SaveAsync();
            return receipt;
        }

        public async Task<Receipt> CreateReceiptWithStoreAsync(CreateReceiptRequest request, int userId)
        {
            // Store handling - find existing or create new
            Store? store = null;
            if (!string.IsNullOrEmpty(request.StoreName))
            {
                store = await _context.Stores.FirstOrDefaultAsync(s =>
                    s.Name.ToLower() == request.StoreName.ToLower());

                if (store == null)
                {
                    store = new Store { Name = request.StoreName };
                    _context.Stores.Add(store);
                    await _context.SaveChangesAsync();
                }
            }

            var receipt = new Receipt
            {
                UserId = userId,
                StoreName = request.StoreName,
                Date = request.Date,
                TotalAmount = request.TotalAmount,
                ImagePath = request.ImagePath,
                CategoryId = request.CategoryId,
                StoreId = store?.Id,
                Items = request.Items.Select(item => new ReceiptItems
                {
                    ProductName = item.ProductName,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Barcode = item.Barcode,
                    Unit = item.Unit
                }).ToList()
            };

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


        public List<ReceiptItems> ParseReceiptItems(string ocrText)
        {
            var items = new List<ReceiptItems>();
            if (string.IsNullOrEmpty(ocrText)) return items;

            var lines = ocrText.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

            // Pattern to match product lines
            // Group 1: Barcode (optional digits)
            // Group 2: Quantity (number with optional comma)
            // Group 3: Unit (AD or KG)
            // Group 4: Unit price (number with comma)
            // The product name follows on same or next line
            // Total price is after * symbol

            var itemPattern = new Regex(@"^(\d+)?\s*(\d+(?:,\d+)?)\s*(AD|PK|KG)\s*[x×]\s*(\d+(?:,\d+)?)", RegexOptions.IgnoreCase);
            var pricePattern = new Regex(@"\*\s*(\d+(?:,\d+)?)");

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var match = itemPattern.Match(line);

                if (match.Success)
                {
                    var barcode = match.Groups[1].Success ? match.Groups[1].Value : null;
                    var quantityStr = match.Groups[2].Value.Replace(',', '.');
                    var unit = match.Groups[3].Value.ToUpper();
                    var unitPriceStr = match.Groups[4].Value.Replace(',', '.');

                    decimal.TryParse(quantityStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal quantity);
                    decimal.TryParse(unitPriceStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal unitPrice);

                    // Find product name - usually on the same or next line after the pattern
                    string productName = "";

                    // Check next line for product name (usually uppercase)
                    if (i + 1 < lines.Count)
                    {
                        var nextLine = lines[i + 1];
                        // Product name line typically doesn't start with numbers or special chars
                        if (!Regex.IsMatch(nextLine, @"^[\d\*]") && !nextLine.Contains("Toplam") && !nextLine.Contains("KDV"))
                        {
                            productName = nextLine.Split('%')[0].Trim(); // Remove tax info
                            i++; // Skip this line in next iteration
                        }
                    }

                    // If no product name found, extract from current line
                    if (string.IsNullOrEmpty(productName))
                    {
                        var afterMatch = line.Substring(match.Index + match.Length);
                        productName = afterMatch.Split('%')[0].Trim();
                    }

                    // Find total price (after *)
                    decimal totalPrice = 0;
                    var priceMatch = pricePattern.Match(line);
                    if (!priceMatch.Success && i + 1 < lines.Count)
                    {
                        priceMatch = pricePattern.Match(lines[i + 1]);
                    }

                    if (priceMatch.Success)
                    {
                        var priceStr = priceMatch.Groups[1].Value.Replace(',', '.');
                        decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out totalPrice);
                    }
                    else
                    {
                        // Calculate from quantity and unit price
                        totalPrice = quantity * unitPrice;
                    }

                    // Clean up product name
                    productName = Regex.Replace(productName, @"-[A-Z]$", "").Trim(); // Remove "-B" suffix

                    if (!string.IsNullOrEmpty(productName) && totalPrice > 0)
                    {
                        items.Add(new ReceiptItems
                        {
                            ProductName = productName,
                            Price = totalPrice,
                            Quantity = quantity,
                            UnitPrice = unitPrice,
                            Barcode = barcode,
                            Unit = unit == "PK" ? "AD" : unit
                        });
                    }
                }
            }

            return items;
        }
    }
}

