using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/ai-usage")]
    [Authorize(AuthenticationSchemes = "Clerk")]
    public class AiUsageController : FinmateControllerBase
    {
        private readonly AiUsageService _aiUsageService;
        private readonly ILogger<AiUsageController> _logger;

        public AiUsageController(
            UserService userService,
            AiUsageService aiUsageService,
            ILogger<AiUsageController> logger)
            : base(userService)
        {
            _aiUsageService = aiUsageService;
            _logger = logger;
        }

        /// <summary>Lượt dùng AI (lập kế hoạch / chatbot) trong tháng hiện tại.</summary>
        [HttpGet]
        public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                var snapshot = await _aiUsageService.GetSnapshotAsync(userId.Value, cancellationToken);
                return Ok(snapshot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading AI usage snapshot");
                return StatusCode(500, new { message = "Không tải được thống kê AI." });
            }
        }
    }
}
