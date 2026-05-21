using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using smart_receipt_api.DTOs;
using smart_receipt_api.Models;
using smart_receipt_api.Services;

namespace smart_receipt_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Tags("Categories")]
    public class CategoriesController : BaseApiController
    {
        private const string SystemDefaultMessage = "System default categories cannot be modified.";

        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        /// <summary>Lists system default and current-user categories.</summary>
        /// <response code="200">The visible categories were returned.</response>
        /// <response code="401">The request is not authenticated.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<CategoryDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<List<CategoryDto>> { Success = false, Message = "Unauthorized." });

                var categories = await _categoryService.GetVisibleAsync(userId);
                return Ok(new ApiResponse<List<CategoryDto>>
                {
                    Success = true,
                    Data = categories.Select(MapToDto).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get categories.");
                return StatusCode(500, new ApiResponse<List<CategoryDto>> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Gets one visible category by id.</summary>
        /// <param name="id">Category id.</param>
        /// <response code="200">The category was returned.</response>
        /// <response code="404">The category is not visible to the current user.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(int id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<CategoryDto> { Success = false, Message = "Unauthorized." });

                var category = await _categoryService.GetByIdAsync(id, userId);
                if (category == null)
                    return NotFound(new ApiResponse<CategoryDto> { Success = false, Message = "Category not found." });

                return Ok(new ApiResponse<CategoryDto> { Success = true, Data = MapToDto(category) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get category.");
                return StatusCode(500, new ApiResponse<CategoryDto> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Creates a user-owned category.</summary>
        /// <param name="request">Category fields.</param>
        /// <response code="201">The category was created.</response>
        /// <response code="400">The request is invalid.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory(UpsertCategoryRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<CategoryDto> { Success = false, Message = "Unauthorized." });

                var errors = ValidateRequest(request);
                if (errors.Count > 0)
                    return BadRequest(new ApiResponse<CategoryDto> { Success = false, Message = "Validation failed.", Errors = errors });

                var category = await _categoryService.CreateAsync(userId, request);
                var dto = MapToDto(category);

                return CreatedAtAction(nameof(GetCategory), new { id = category.Id },
                    new ApiResponse<CategoryDto> { Success = true, Message = "Category created.", Data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create category.");
                return StatusCode(500, new ApiResponse<CategoryDto> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Updates a user-owned category.</summary>
        /// <param name="id">Category id.</param>
        /// <param name="request">Updated category fields.</param>
        /// <response code="200">The category was updated.</response>
        /// <response code="400">The request is invalid or targets a system default.</response>
        /// <response code="404">The category is not visible to the current user.</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(int id, UpsertCategoryRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<CategoryDto> { Success = false, Message = "Unauthorized." });

                var category = await _categoryService.GetByIdAsync(id, userId);
                if (category == null)
                    return NotFound(new ApiResponse<CategoryDto> { Success = false, Message = "Category not found." });
                if (category.UserId == null)
                    return BadRequest(new ApiResponse<CategoryDto> { Success = false, Message = SystemDefaultMessage });

                var errors = ValidateRequest(request);
                if (errors.Count > 0)
                    return BadRequest(new ApiResponse<CategoryDto> { Success = false, Message = "Validation failed.", Errors = errors });

                var updated = await _categoryService.UpdateAsync(id, userId, request);
                return Ok(new ApiResponse<CategoryDto> { Success = true, Message = "Category updated.", Data = MapToDto(updated!) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update category.");
                return StatusCode(500, new ApiResponse<CategoryDto> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Deletes a user-owned category.</summary>
        /// <param name="id">Category id.</param>
        /// <response code="200">The category was deleted.</response>
        /// <response code="400">The request targets a system default.</response>
        /// <response code="404">The category is not visible to the current user.</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(int id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "Unauthorized." });

                var category = await _categoryService.GetByIdAsync(id, userId);
                if (category == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Category not found." });
                if (category.UserId == null)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = SystemDefaultMessage });

                await _categoryService.DeleteAsync(id, userId);
                return Ok(new ApiResponse<object> { Success = true, Message = "Category deleted.", Data = null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete category.");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Sets or clears a monthly budget for a user-owned category.</summary>
        /// <param name="id">Category id.</param>
        /// <param name="request">Budget limit. Null clears the budget.</param>
        /// <response code="200">The category budget was updated.</response>
        /// <response code="400">The request is invalid or targets a system default.</response>
        /// <response code="404">The category is not visible to the current user.</response>
        [HttpPut("{id:int}/budget")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> SetBudget(int id, SetBudgetRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<CategoryDto> { Success = false, Message = "Unauthorized." });

                var category = await _categoryService.GetByIdAsync(id, userId);
                if (category == null)
                    return NotFound(new ApiResponse<CategoryDto> { Success = false, Message = "Category not found." });
                if (category.UserId == null)
                    return BadRequest(new ApiResponse<CategoryDto> { Success = false, Message = SystemDefaultMessage });
                if (request.MonthlyBudgetLimit is < 0)
                    return BadRequest(new ApiResponse<CategoryDto> { Success = false, Message = "Budget limit cannot be negative." });

                var updated = await _categoryService.SetBudgetAsync(id, userId, request.MonthlyBudgetLimit);
                return Ok(new ApiResponse<CategoryDto> { Success = true, Message = "Budget updated.", Data = MapToDto(updated!) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set category budget.");
                return StatusCode(500, new ApiResponse<CategoryDto> { Success = false, Message = "An error occurred." });
            }
        }

        private static CategoryDto MapToDto(Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                IconUrl = category.IconUrl,
                MonthlyBudgetLimit = category.MonthlyBudgetLimit,
                IsSystemDefault = category.UserId == null
            };
        }

        private static List<string> ValidateRequest(UpsertCategoryRequest request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Name))
                errors.Add("Category name is required.");
            else if (request.Name.Trim().Length is < 2 or > 50)
                errors.Add("Category name must be between 2 and 50 characters.");

            if (request.MonthlyBudgetLimit is < 0)
                errors.Add("Budget limit cannot be negative.");

            return errors;
        }
    }
}
