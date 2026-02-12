using BLL.DTOs.Response;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(AuthenticationSchemes = "Clerk,Basic")]
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
            // Ưu tiên đọc userId (Guid) từ JWT basic
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("userId")?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            // Fallback: token từ Clerk, map sang user trong DB
            var clerkUserId = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(clerkUserId))
            {
                var clerkUserDto = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
                return clerkUserDto?.Id;
            }

            return null;
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
