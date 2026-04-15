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
    [Route("api/admin/dashboard-summary")]
    public class AdminDashboardSummaryController : ControllerBase
    {
        private readonly FinmateContext _db;
        private readonly ClerkService _clerkService;
        private readonly UserService _userService;
        private readonly ILogger<AdminDashboardSummaryController> _logger;

        public AdminDashboardSummaryController(
            FinmateContext db,
            ClerkService clerkService,
            UserService userService,
            ILogger<AdminDashboardSummaryController> logger)
        {
            _db = db;
            _clerkService = clerkService;
            _userService = userService;
            _logger = logger;
        }

        private async Task<(Users? me, IActionResult? error)> RequireStaffOrAdminAsync()
        {
            var authHeader = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return (null, Unauthorized("Missing Bearer token"));

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var (clerkUserId, err) = await _clerkService.VerifyTokenAndGetUserIdWithErrorAsync(token);
            if (string.IsNullOrWhiteSpace(clerkUserId))
                return (null, Unauthorized(err ?? "Invalid token"));

            var meDto = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
            if (meDto == null) return (null, Unauthorized("User not found"));
            if ((int)meDto.Role < (int)Role.Staff) return (null, Forbid());

            var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == meDto.Id);
            if (me == null) return (null, Unauthorized("User not found"));

            return (me, null);
        }

        public class AdminDashboardSummaryDto
        {
            public PremiumOrderStatsDto PremiumOrders { get; set; } = new();
            public decimal PremiumRevenueVndThisMonth { get; set; }
            public decimal TotalSystemIncomeVnd { get; set; }
            public int CustomersWithGoals { get; set; }
            public int PremiumExpiringIn5Days { get; set; }
            public UserStatsDto Users { get; set; } = new();
            public AiUsageAdminDto AiUsage { get; set; } = new();
            public int PremiumPlanConfigsActive { get; set; }
            public DateTime GeneratedAtUtc { get; set; }
        }

        public class PremiumOrderStatsDto
        {
            public int Pending { get; set; }
            public int Paid { get; set; }
            public int Expired { get; set; }
            public int Cancelled { get; set; }
            public int Total { get; set; }
        }

        public class UserStatsDto
        {
            public int Total { get; set; }
            public int Premium { get; set; }
            public int StaffOrAdmin { get; set; }
        }

        public class AiUsageAdminDto
        {
            public string PeriodKey { get; set; } = string.Empty;
            public int TotalPlanCalls { get; set; }
            public int TotalChatCalls { get; set; }
            public int UsersWithUsage { get; set; }
        }

        /// <summary>Tong hop nhanh cho Staff/Admin (premium, user, AI usage thang UTC).</summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get()
        {
            try
            {
                var (_, err) = await RequireStaffOrAdminAsync();
                if (err != null) return err;

                var periodKey = DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var monthEnd = monthStart.AddMonths(1);

                var pending = await _db.PremiumOrders.AsNoTracking().CountAsync(o => o.Status == "Pending");
                var paid = await _db.PremiumOrders.AsNoTracking().CountAsync(o => o.Status == "Paid");
                var expired = await _db.PremiumOrders.AsNoTracking().CountAsync(o => o.Status == "Expired");
                var cancelled = await _db.PremiumOrders.AsNoTracking().CountAsync(o => o.Status == "Cancelled");
                var ordersTotal = await _db.PremiumOrders.AsNoTracking().CountAsync();

                var revenueMonth = await _db.PremiumOrders.AsNoTracking()
                    .Where(o => o.Status == "Paid" && o.PaidAt != null && o.PaidAt >= monthStart && o.PaidAt < monthEnd)
                    .SumAsync(o => o.AmountVnd);

                var totalIncomeVnd = await _db.Transactions.AsNoTracking()
                    .Join(_db.TransactionTypes.AsNoTracking(),
                        t => t.TransactionTypeId,
                        tt => tt.Id,
                        (t, tt) => new { t, tt })
                    .Where(x => x.tt.IsIncome && !x.t.ExcludeFromReport)
                    .Select(x => (decimal?)x.t.Amount)
                    .SumAsync() ?? 0m;

                var customersWithGoals = await _db.Goals.AsNoTracking()
                    .Select(g => g.UserId)
                    .Distinct()
                    .CountAsync();

                var now = DateTime.UtcNow;
                var fiveDaysLater = now.AddDays(5);
                var premiumExpiringIn5Days = await _db.PremiumSubscriptions.AsNoTracking()
                    .Where(s => s.IsActive && s.ExpiresAt > now && s.ExpiresAt <= fiveDaysLater)
                    .Select(s => s.UserId)
                    .Distinct()
                    .CountAsync();

                var usersTotal = await _db.Users.AsNoTracking().CountAsync();
                var usersPremium = await _db.Users.AsNoTracking().CountAsync(u => u.IsPremium);
                var staffOrAdmin = await _db.Users.AsNoTracking().CountAsync(u => (int)u.Role >= (int)Role.Staff);

                var aiPlanSum = await _db.UserAiMonthlyUsages.AsNoTracking()
                    .Where(x => x.PeriodKey == periodKey)
                    .Select(x => (int?)x.PlanCalls)
                    .SumAsync() ?? 0;

                var aiChatSum = await _db.UserAiMonthlyUsages.AsNoTracking()
                    .Where(x => x.PeriodKey == periodKey)
                    .Select(x => (int?)x.ChatCalls)
                    .SumAsync() ?? 0;

                var aiUsers = await _db.UserAiMonthlyUsages.AsNoTracking()
                    .CountAsync(x => x.PeriodKey == periodKey && (x.PlanCalls > 0 || x.ChatCalls > 0));

                var activePlans = await _db.PremiumPlanConfigs.AsNoTracking().CountAsync(c => c.IsActive);

                var dto = new AdminDashboardSummaryDto
                {
                    PremiumOrders = new PremiumOrderStatsDto
                    {
                        Pending = pending,
                        Paid = paid,
                        Expired = expired,
                        Cancelled = cancelled,
                        Total = ordersTotal,
                    },
                    PremiumRevenueVndThisMonth = revenueMonth,
                    TotalSystemIncomeVnd = totalIncomeVnd,
                    CustomersWithGoals = customersWithGoals,
                    PremiumExpiringIn5Days = premiumExpiringIn5Days,
                    Users = new UserStatsDto
                    {
                        Total = usersTotal,
                        Premium = usersPremium,
                        StaffOrAdmin = staffOrAdmin,
                    },
                    AiUsage = new AiUsageAdminDto
                    {
                        PeriodKey = periodKey,
                        TotalPlanCalls = aiPlanSum,
                        TotalChatCalls = aiChatSum,
                        UsersWithUsage = aiUsers,
                    },
                    PremiumPlanConfigsActive = activePlans,
                    GeneratedAtUtc = DateTime.UtcNow,
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building admin dashboard summary");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
