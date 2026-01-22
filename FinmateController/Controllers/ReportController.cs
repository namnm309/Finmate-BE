using BLL.DTOs.Response;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly ReportService _reportService;
        private readonly UserService _userService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            ReportService reportService,
            UserService userService,
            ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _userService = userService;
            _logger = logger;
        }

        private async Task<Guid?> GetCurrentUserIdAsync()
        {
            var clerkUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(clerkUserId))
            {
                return null;
            }

            var user = await _userService.GetUserByClerkIdAsync(clerkUserId);
            return user?.Id;
        }

        /// <summary>
        /// Lấy báo cáo tổng quan thu/chi và thống kê theo danh mục
        /// </summary>
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "Invalid user" });
                }

                var result = await _reportService.GetOverviewAsync(userId.Value, startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overview report");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
