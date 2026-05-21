using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using smart_receipt_api.DTOs;
using smart_receipt_api.Services;

namespace smart_receipt_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Tags("Budgets")]
    public class BudgetsController : BaseApiController
    {
        private readonly IBudgetService _budgetService;
        private readonly ILogger<BudgetsController> _logger;

        public BudgetsController(IBudgetService budgetService, ILogger<BudgetsController> logger)
        {
            _budgetService = budgetService;
            _logger = logger;
        }

        /// <summary>Gets per-category spending against monthly budget limits.</summary>
        /// <param name="year">Optional year. Defaults to the current UTC year.</param>
        /// <param name="month">Optional month. Defaults to the current UTC month.</param>
        /// <response code="200">Budget statuses were returned.</response>
        [HttpGet("status")]
        [ProducesResponseType(typeof(ApiResponse<List<BudgetStatusDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<BudgetStatusDto>>>> GetStatus([FromQuery] int? year, [FromQuery] int? month)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<List<BudgetStatusDto>> { Success = false, Message = "Unauthorized." });

                var statuses = await _budgetService.GetStatusAsync(userId, year, month);
                return Ok(new ApiResponse<List<BudgetStatusDto>> { Success = true, Data = statuses });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get budget statuses.");
                return StatusCode(500, new ApiResponse<List<BudgetStatusDto>> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Gets categories that are over budget for the selected month.</summary>
        /// <param name="year">Optional year. Defaults to the current UTC year.</param>
        /// <param name="month">Optional month. Defaults to the current UTC month.</param>
        /// <response code="200">Budget alerts were returned.</response>
        [HttpGet("alerts")]
        [ProducesResponseType(typeof(ApiResponse<List<BudgetStatusDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<BudgetStatusDto>>>> GetAlerts([FromQuery] int? year, [FromQuery] int? month)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<List<BudgetStatusDto>> { Success = false, Message = "Unauthorized." });

                var alerts = await _budgetService.GetAlertsAsync(userId, year, month);
                return Ok(new ApiResponse<List<BudgetStatusDto>> { Success = true, Data = alerts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get budget alerts.");
                return StatusCode(500, new ApiResponse<List<BudgetStatusDto>> { Success = false, Message = "An error occurred." });
            }
        }

        /// <summary>Gets a monthly budget summary and per-category statuses.</summary>
        /// <param name="year">Optional year. Defaults to the current UTC year.</param>
        /// <param name="month">Optional month. Defaults to the current UTC month.</param>
        /// <response code="200">Budget summary was returned.</response>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse<BudgetSummaryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<BudgetSummaryDto>>> GetSummary([FromQuery] int? year, [FromQuery] int? month)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                    return Unauthorized(new ApiResponse<BudgetSummaryDto> { Success = false, Message = "Unauthorized." });

                var summary = await _budgetService.GetSummaryAsync(userId, year, month);
                return Ok(new ApiResponse<BudgetSummaryDto> { Success = true, Data = summary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get budget summary.");
                return StatusCode(500, new ApiResponse<BudgetSummaryDto> { Success = false, Message = "An error occurred." });
            }
        }
    }
}
