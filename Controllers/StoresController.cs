using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_receipt_api.DTOs;
using smart_receipt_api.Models;

namespace smart_receipt_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StoresController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StoresController> _logger;

        public StoresController(AppDbContext context, ILogger<StoresController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<StoreDto>>>> GetStores()
        {
            try
            {
                var stores = await _context.Stores
                    .Select(s => new StoreDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Address = s.Address,
                        Phone = s.Phone,
                        TaxNumber = s.TaxNumber
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<StoreDto>> { Success = true, Data = stores });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get stores error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<StoreDto>>> GetStore(int id)
        {
            try
            {
                var store = await _context.Stores.FindAsync(id);
                if (store == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Store not found" });

                var dto = new StoreDto
                {
                    Id = store.Id,
                    Name = store.Name,
                    Address = store.Address,
                    Phone = store.Phone,
                    TaxNumber = store.TaxNumber
                };

                return Ok(new ApiResponse<StoreDto> { Success = true, Data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get store error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<StoreDto>>> CreateStore(CreateStoreRequest request)
        {
            try
            {
                var store = new Store
                {
                    Name = request.Name,
                    Address = request.Address,
                    Phone = request.Phone,
                    TaxNumber = request.TaxNumber
                };

                _context.Stores.Add(store);
                await _context.SaveChangesAsync();

                var dto = new StoreDto
                {
                    Id = store.Id,
                    Name = store.Name,
                    Address = store.Address,
                    Phone = store.Phone,
                    TaxNumber = store.TaxNumber
                };

                return CreatedAtAction(nameof(GetStore), new { id = store.Id },
                    new ApiResponse<StoreDto> { Success = true, Data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Create store error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<StoreDto>>> UpdateStore(int id, CreateStoreRequest request)
        {
            try
            {
                var store = await _context.Stores.FindAsync(id);
                if (store == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Store not found" });

                store.Name = request.Name;
                store.Address = request.Address;
                store.Phone = request.Phone;
                store.TaxNumber = request.TaxNumber;

                await _context.SaveChangesAsync();

                var dto = new StoreDto
                {
                    Id = store.Id,
                    Name = store.Name,
                    Address = store.Address,
                    Phone = store.Phone,
                    TaxNumber = store.TaxNumber
                };

                return Ok(new ApiResponse<StoreDto> { Success = true, Data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update store error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStore(int id)
        {
            try
            {
                var store = await _context.Stores.FindAsync(id);
                if (store == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Store not found" });

                _context.Stores.Remove(store);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object> { Success = true, Message = "Store deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete store error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }
    }
}

