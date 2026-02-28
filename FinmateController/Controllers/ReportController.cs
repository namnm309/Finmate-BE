using BLL.DTOs.Response;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(AuthenticationSchemes = "Clerk,Basic")]
    public class ReportController : FinmateControllerBase
    {
        private readonly ReportService _reportService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            ReportService reportService,
            UserService userService,
            ILogger<ReportController> logger)
            : base(userService)
        {
            _reportService = reportService;
            _logger = logger;
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
