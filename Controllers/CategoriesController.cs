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
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(AppDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        IconName = c.IconName
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<CategoryDto>> { Success = true, Data = categories });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get categories error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Category not found" });

                var dto = new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IconName = category.IconName
                };

                return Ok(new ApiResponse<CategoryDto> { Success = true, Data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get category error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory(CreateCategoryRequest request)
        {
            try
            {
                var category = new Category
                {
                    Name = request.Name,
                    Description = request.Description,
                    IconName = request.IconName
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                var dto = new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IconName = category.IconName
                };

                return CreatedAtAction(nameof(GetCategory), new { id = category.Id },
                    new ApiResponse<CategoryDto> { Success = true, Data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Create category error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(int id, CreateCategoryRequest request)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Category not found" });

                category.Name = request.Name;
                category.Description = request.Description;
                category.IconName = request.IconName;

                await _context.SaveChangesAsync();

                var dto = new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IconName = category.IconName
                };

                return Ok(new ApiResponse<CategoryDto> { Success = true, Data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update category error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Category not found" });

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object> { Success = true, Message = "Category deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete category error: {ex.Message}");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred" });
            }
        }
    }
}

