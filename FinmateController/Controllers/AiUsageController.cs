using BLL.Services;
using DAL.Data;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/ai-usage")]
    [Authorize(AuthenticationSchemes = "Clerk")]
    public class AiUsageController : FinmateControllerBase
    {
        private readonly FinmateContext _db;
        private readonly AiUsageService _aiUsageService;
        private readonly ILogger<AiUsageController> _logger;

        public AiUsageController(
            FinmateContext db,
            UserService userService,
            AiUsageService aiUsageService,
            ILogger<AiUsageController> logger)
            : base(userService)
        {
            _db = db;
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

        public class AiUsageOverallDto
        {
            public int UsersWithUsage { get; set; }
            public int TotalPlanCalls { get; set; }
            public int TotalChatCalls { get; set; }
            public int TotalCalls { get; set; }
            public DateTime GeneratedAtUtc { get; set; }
        }

        /// <summary>
        /// Thống kê AI toàn hệ thống từ lúc app vận hành tới hiện tại.
        /// Chỉ Staff/Admin được xem.
        /// </summary>
        [HttpGet("overall")]
        public async Task<IActionResult> GetOverall(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
                if (me == null)
                    return Unauthorized(new { message = "User not authenticated" });
                if ((int)me.Role < (int)Role.Staff)
                    return Forbid();

                var usageRows = await _db.UserAiMonthlyUsages.AsNoTracking()
                    .Select(x => new { x.UserId, x.PlanCalls, x.ChatCalls })
                    .ToListAsync(cancellationToken);

                var totalPlanCalls = usageRows.Sum(x => x.PlanCalls);
                var totalChatCalls = usageRows.Sum(x => x.ChatCalls);
                var usersWithUsage = usageRows
                    .Where(x => x.PlanCalls > 0 || x.ChatCalls > 0)
                    .Select(x => x.UserId)
                    .Distinct()
                    .Count();

                return Ok(new AiUsageOverallDto
                {
                    UsersWithUsage = usersWithUsage,
                    TotalPlanCalls = totalPlanCalls,
                    TotalChatCalls = totalChatCalls,
                    TotalCalls = totalPlanCalls + totalChatCalls,
                    GeneratedAtUtc = DateTime.UtcNow,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading overall AI usage stats");
                return StatusCode(500, new { message = "Không tải được thống kê AI toàn hệ thống." });
            }
        }
    }
}
