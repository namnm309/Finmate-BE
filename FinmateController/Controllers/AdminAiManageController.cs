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

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> List(
            [FromQuery] string? periodKey,
            [FromQuery] string? q,
            [FromQuery] string? sort,
            [FromQuery] string? dir,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20,
            CancellationToken cancellationToken = default)
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

                var usersQuery = _db.Users.AsNoTracking();
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var pattern = $"%{q.Trim()}%";
                    usersQuery = usersQuery.Where(u =>
                        (u.Email != null && EF.Functions.ILike(u.Email, pattern)) ||
                        (u.FullName != null && EF.Functions.ILike(u.FullName, pattern)));
                }

                var users = await usersQuery
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FullName,
                        u.IsPremium,
                        Role = (int)u.Role,
                    })
                    .ToListAsync(cancellationToken);

                var goalsMap = await _db.Goals.AsNoTracking()
                    .GroupBy(g => g.UserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

                var usageRows = await _db.UserAiMonthlyUsages.AsNoTracking()
                    .Where(x => x.PeriodKey == period)
                    .Select(x => new { x.UserId, x.ChatCalls, x.PlanCalls })
                    .ToListAsync(cancellationToken);

                var usageMap = usageRows.ToDictionary(x => x.UserId, x => (x.ChatCalls, x.PlanCalls));

                var rows = users.Select(u =>
                {
                    usageMap.TryGetValue(u.Id, out var usage);
                    goalsMap.TryGetValue(u.Id, out var goalsCount);
                    return new AiManageUserRowDto
                    {
                        UserId = u.Id,
                        Email = u.Email ?? string.Empty,
                        FullName = u.FullName ?? string.Empty,
                        IsPremium = u.IsPremium,
                        Role = u.Role,
                        ChatCalls = usage.ChatCalls,
                        PlanCalls = usage.PlanCalls,
                        GoalsCount = goalsCount,
                    };
                });

                var sortKey = (sort ?? "chat").Trim().ToLowerInvariant();
                var desc = !string.Equals((dir ?? "desc").Trim(), "asc", StringComparison.OrdinalIgnoreCase);
                rows = sortKey switch
                {
                    "plan" => desc ? rows.OrderByDescending(x => x.PlanCalls).ThenByDescending(x => x.ChatCalls) : rows.OrderBy(x => x.PlanCalls).ThenBy(x => x.ChatCalls),
                    "goals" => desc ? rows.OrderByDescending(x => x.GoalsCount).ThenByDescending(x => x.ChatCalls) : rows.OrderBy(x => x.GoalsCount).ThenBy(x => x.ChatCalls),
                    "email" => desc ? rows.OrderByDescending(x => x.Email) : rows.OrderBy(x => x.Email),
                    _ => desc ? rows.OrderByDescending(x => x.ChatCalls).ThenByDescending(x => x.PlanCalls) : rows.OrderBy(x => x.ChatCalls).ThenBy(x => x.PlanCalls),
                };

                var total = rows.Count();
                var items = rows.Skip((page - 1) * perPage).Take(perPage).ToList();

                var totalChat = usageRows.Sum(x => x.ChatCalls);
                var totalPlan = usageRows.Sum(x => x.PlanCalls);
                var usersWithUsage = usageRows.Count(x => x.ChatCalls > 0 || x.PlanCalls > 0);

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
