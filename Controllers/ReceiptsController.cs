using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_receipt_api.Data;
using smart_receipt_api.Models;

namespace smart_receipt_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReceiptsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReceiptsController(AppDbContext context)
        {
            _context = context;
        }

        // (GET: api/receipts) get all receipts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Receipt>>> GetReceipts()
        {
            return await _context.Receipts.OrderByDescending(r => r.Date).ToListAsync();
        }

        // POST: api/receipts) add new receipt
        [HttpPost]
        public async Task<ActionResult<Receipt>> PostReceipt(Receipt receipt)
        {
            // TODO: temporary user id = 1 for test reasons
            if (receipt.UserId == 0) receipt.UserId = 1;

            _context.Receipts.Add(receipt);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetReceipts", new { id = receipt.Id }, receipt);
        }

        // (GET: api/dashboard-stats) dashboard statistics
        [HttpGet("dashboard-stats")]
        public async Task<ActionResult<object>> GetDashboardStats()
        {
            var totalSpending = await _context.Receipts.SumAsync(r => r.TotalAmount);
            var receiptCount = await _context.Receipts.CountAsync();

            // En çok harcama yapılan marketi bulalım
            var topStore = await _context.Receipts
                .GroupBy(r => r.StoreName)
                .OrderByDescending(g => g.Sum(x => x.TotalAmount))
                .Select(g => new { StoreName = g.Key, Total = g.Sum(x => x.TotalAmount) })
                .FirstOrDefaultAsync();

            return new
            {
                TotalSpending = totalSpending,
                ReceiptCount = receiptCount,
                TopStore = topStore?.StoreName ?? "No data yet.",
                CurrentMonth = DateTime.Now.ToString("MMMM yyyy")
            };
        }
    }
}