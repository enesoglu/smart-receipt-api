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
    [Tags("Stores")]
    public class StoresController : BaseApiController
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StoresController> _logger;

        public StoresController(AppDbContext context, ILogger<StoresController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>Lists stores known to the system.</summary>
        /// <response code="200">Stores were returned.</response>
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
                _logger.LogError(ex, "Failed to get stores.");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Gets a store by id.</summary>
        /// <param name="id">Store id.</param>
        /// <response code="200">The store was returned.</response>
        /// <response code="404">The store was not found.</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<StoreDto>>> GetStore(int id)
        {
            try
            {
                var store = await _context.Stores.FindAsync(id);
                if (store == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Store not found." });

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
                _logger.LogError(ex, "Failed to get store.");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Creates a store.</summary>
        /// <param name="request">Store fields.</param>
        /// <response code="201">The store was created.</response>
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
                _logger.LogError(ex, "Failed to create store.");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Updates a store.</summary>
        /// <param name="id">Store id.</param>
        /// <param name="request">Updated store fields.</param>
        /// <response code="200">The store was updated.</response>
        /// <response code="404">The store was not found.</response>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<StoreDto>>> UpdateStore(int id, CreateStoreRequest request)
        {
            try
            {
                var store = await _context.Stores.FindAsync(id);
                if (store == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Store not found." });

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
                _logger.LogError(ex, "Failed to update store.");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Deletes a store.</summary>
        /// <param name="id">Store id.</param>
        /// <response code="200">The store was deleted.</response>
        /// <response code="404">The store was not found.</response>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStore(int id)
        {
            try
            {
                var store = await _context.Stores.FindAsync(id);
                if (store == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Store not found." });

                _context.Stores.Remove(store);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object> { Success = true, Message = "Store deleted." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete store.");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred." });
            }
        }
    }
}

