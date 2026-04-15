using BLL.Services;
using DAL.Data;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/admin/ai-manage")]
    public class AdminAiManageController : ControllerBase
    {
        private readonly FinmateContext _db;
        private readonly ClerkService _clerkService;
        private readonly UserService _userService;
        private readonly ILogger<AdminAiManageController> _logger;

        public AdminAiManageController(
            FinmateContext db,
            ClerkService clerkService,
            UserService userService,
            ILogger<AdminAiManageController> logger)
        {
            _db = db;
            _clerkService = clerkService;
            _userService = userService;
            _logger = logger;
        }

        private async Task<IActionResult?> RequireStaffOrAdminAsync()
        {
            var authHeader = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Unauthorized("Missing Bearer token");

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var (clerkUserId, err) = await _clerkService.VerifyTokenAndGetUserIdWithErrorAsync(token);
            if (string.IsNullOrWhiteSpace(clerkUserId))
                return Unauthorized(err ?? "Invalid token");

            var meDto = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
            if (meDto == null) return Unauthorized("User not found");
            if ((int)meDto.Role < (int)Role.Staff) return Forbid();

            return null;
        }

        public class AiManageSummaryDto
        {
            public string PeriodKey { get; set; } = string.Empty;
            public int TotalChatCalls { get; set; }
            public int TotalPlanCalls { get; set; }
            public int UsersWithUsage { get; set; }
            public DateTime GeneratedAtUtc { get; set; }
        }

        public class AiManageUserRowDto
        {
            public Guid UserId { get; set; }
            public string Email { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public bool IsPremium { get; set; }
            public int Role { get; set; }
            public int ChatCalls { get; set; }
            public int PlanCalls { get; set; }
            public int GoalsCount { get; set; }
        }

        public class PagedResponse<T>
        {
            public List<T> Items { get; set; } = new();
            public int Page { get; set; }
            public int PerPage { get; set; }
            public int Total { get; set; }
            public AiManageSummaryDto Summary { get; set; } = new();
        }

        /// <summary>
        /// Danh sach usage AI theo user (theo thang UTC). Staff/Admin only.
        /// Query:
        /// - periodKey: yyyy-MM (mac dinh thang hien tai UTC)
        /// - q: tim theo email/fullName
        /// - sort: chat|plan|goals|email (mac dinh chat)
        /// - dir: desc|asc (mac dinh desc)
        /// - page/perPage
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> List(
            [FromQuery] string? periodKey,
            [FromQuery] string? q,
            [FromQuery] string? sort,
            [FromQuery] string? dir,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20)
        {
            try
            {
                var authErr = await RequireStaffOrAdminAsync();
                if (authErr != null) return authErr;

                var period = string.IsNullOrWhiteSpace(periodKey)
                    ? DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture)
                    : periodKey.Trim();

                page = page < 1 ? 1 : page;
                perPage = perPage < 1 ? 20 : perPage;
                perPage = Math.Min(perPage, 200);

                // Base users query (so we can show 0 usage too if needed later).
                var usersQuery = _db.Users.AsNoTracking();
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var qq = q.Trim().ToLowerInvariant();
                    usersQuery = usersQuery.Where(u =>
                        (u.Email != null && u.Email.ToLower().Contains(qq)) ||
                        (u.FullName != null && u.FullName.ToLower().Contains(qq)));
                }

                // Usage for the period
                var usageQuery = _db.UserAiMonthlyUsages.AsNoTracking()
                    .Where(x => x.PeriodKey == period);

                // Precompute totals for summary
                // SUM() in SQL returns NULL on empty set, guard to avoid 500.
                var totalChat = await usageQuery.Select(x => (int?)x.ChatCalls).SumAsync() ?? 0;
                var totalPlan = await usageQuery.Select(x => (int?)x.PlanCalls).SumAsync() ?? 0;
                var usersWithUsage = await usageQuery.CountAsync(x => x.ChatCalls > 0 || x.PlanCalls > 0);

                // Join users with usage (LEFT JOIN)
                var joined =
                    from u in usersQuery
                    join us in usageQuery on u.Id equals us.UserId into gj
                    from us in gj.DefaultIfEmpty()
                    select new
                    {
                        u.Id,
                        u.Email,
                        u.FullName,
                        u.IsPremium,
                        Role = (int)u.Role,
                        ChatCalls = us != null ? us.ChatCalls : 0,
                        PlanCalls = us != null ? us.PlanCalls : 0,
                    };

                // Goals count per user (grouped)
                var goalsByUser = _db.Goals.AsNoTracking()
                    .GroupBy(g => g.UserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() });

                var finalQuery =
                    from x in joined
                    join g in goalsByUser on x.Id equals g.UserId into gg
                    from g in gg.DefaultIfEmpty()
                    select new AiManageUserRowDto
                    {
                        UserId = x.Id,
                        Email = x.Email ?? "",
                        FullName = x.FullName ?? "",
                        IsPremium = x.IsPremium,
                        Role = x.Role,
                        ChatCalls = x.ChatCalls,
                        PlanCalls = x.PlanCalls,
                        GoalsCount = g != null ? g.Count : 0,
                    };

                // Sorting
                var sortKey = (sort ?? "chat").Trim().ToLowerInvariant();
                var desc = !string.Equals((dir ?? "desc").Trim(), "asc", StringComparison.OrdinalIgnoreCase);

                finalQuery = sortKey switch
                {
                    "plan" => desc ? finalQuery.OrderByDescending(x => x.PlanCalls).ThenByDescending(x => x.ChatCalls) : finalQuery.OrderBy(x => x.PlanCalls).ThenBy(x => x.ChatCalls),
                    "goals" => desc ? finalQuery.OrderByDescending(x => x.GoalsCount).ThenByDescending(x => x.ChatCalls) : finalQuery.OrderBy(x => x.GoalsCount).ThenBy(x => x.ChatCalls),
                    "email" => desc ? finalQuery.OrderByDescending(x => x.Email) : finalQuery.OrderBy(x => x.Email),
                    _ => desc ? finalQuery.OrderByDescending(x => x.ChatCalls).ThenByDescending(x => x.PlanCalls) : finalQuery.OrderBy(x => x.ChatCalls).ThenBy(x => x.PlanCalls),
                };

                var total = await finalQuery.CountAsync();
                var items = await finalQuery
                    .Skip((page - 1) * perPage)
                    .Take(perPage)
                    .ToListAsync();

                return Ok(new PagedResponse<AiManageUserRowDto>
                {
                    Items = items,
                    Page = page,
                    PerPage = perPage,
                    Total = total,
                    Summary = new AiManageSummaryDto
                    {
                        PeriodKey = period,
                        TotalChatCalls = totalChat,
                        TotalPlanCalls = totalPlan,
                        UsersWithUsage = usersWithUsage,
                        GeneratedAtUtc = DateTime.UtcNow,
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing AI manage");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

