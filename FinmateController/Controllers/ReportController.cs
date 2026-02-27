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
            // Đọc userId từ cả Basic JWT (Guid) và Clerk (string userId)
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("userId")?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(claimValue))
            {
                return null;
            }

            // Trường hợp Basic JWT: NameIdentifier/userId là Guid
            if (Guid.TryParse(claimValue, out var userId))
            {
                return userId;
            }

            // Trường hợp Clerk: NameIdentifier/sub là Clerk User ID (không phải Guid)
            var clerkUser = await _userService.GetOrCreateUserFromClerkAsync(claimValue);
            return clerkUser?.Id;
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
