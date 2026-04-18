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

        public class UserMetricsPointDto
        {
            public string Date { get; set; } = string.Empty; // yyyy-MM-dd (UTC)
            public int ActiveUsers { get; set; }
            public int NewUsers { get; set; }
            public int PremiumBuyers { get; set; }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
        {
            try
            {
                var (_, err) = await RequireStaffOrAdminAsync();
                if (err != null) return err;

                var utcNow = DateTime.UtcNow;
                var periodKey = utcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                var monthStart = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var monthEnd = monthStart.AddMonths(1);

                var pending = await _db.PremiumOrders.AsNoTracking().CountAsync(o => o.Status == "Pending", cancellationToken);
                var paid = await _db.PremiumOrders.AsNoTracking().CountAsync(o => o.Status == "Paid", cancellationToken);
                var expired = await _db.PremiumOrders.AsNoTracking().CountAsync(o => o.Status == "Expired", cancellationToken);
                var cancelled = await _db.PremiumOrders.AsNoTracking().CountAsync(o => o.Status == "Cancelled", cancellationToken);
                var ordersTotal = await _db.PremiumOrders.AsNoTracking().CountAsync(cancellationToken);

                var revenueMonth = await _db.PremiumOrders.AsNoTracking()
                    .Where(o => o.Status == "Paid" && o.PaidAt != null && o.PaidAt >= monthStart && o.PaidAt < monthEnd)
                    .Select(o => (decimal?)o.AmountVnd)
                    .SumAsync(cancellationToken) ?? 0m;

                // Thu nhập từ ví người dùng (bảng Transactions, loại IsIncome).
                var totalIncomeFromTransactionsVnd = await _db.Transactions.AsNoTracking()
                    .Join(_db.TransactionTypes.AsNoTracking(),
                        t => t.TransactionTypeId,
                        tt => tt.Id,
                        (t, tt) => new { t, tt })
                    .Where(x => x.tt.IsIncome && !x.t.ExcludeFromReport)
                    .Select(x => (decimal?)x.t.Amount)
                    .SumAsync(cancellationToken) ?? 0m;

                // Đơn Premium không tạo Transaction; cộng riêng để dashboard phản ánh doanh thu thực tế.
                var totalPremiumPaidAllTimeVnd = await _db.PremiumOrders.AsNoTracking()
                    .Where(o => o.Status == "Paid")
                    .Select(o => (decimal?)o.AmountVnd)
                    .SumAsync(cancellationToken) ?? 0m;

                var totalSystemIncomeVnd = totalIncomeFromTransactionsVnd + totalPremiumPaidAllTimeVnd;

                var customersWithGoals = await _db.Goals.AsNoTracking()
                    .Select(g => g.UserId)
                    .Distinct()
                    .CountAsync(cancellationToken);

                var fiveDaysLater = utcNow.AddDays(5);
                var premiumExpiringIn5Days = await _db.PremiumSubscriptions.AsNoTracking()
                    .Where(s => s.IsActive && s.ExpiresAt > utcNow && s.ExpiresAt <= fiveDaysLater)
                    .Select(s => s.UserId)
                    .Distinct()
                    .CountAsync(cancellationToken);

                var usersTotal = await _db.Users.AsNoTracking().CountAsync(cancellationToken);
                var usersPremium = await _db.Users.AsNoTracking().CountAsync(u => u.IsPremium, cancellationToken);
                var staffOrAdmin = await _db.Users.AsNoTracking().CountAsync(u => (int)u.Role >= (int)Role.Staff, cancellationToken);

                var aiPlanSum = await _db.UserAiMonthlyUsages.AsNoTracking()
                    .Where(x => x.PeriodKey == periodKey)
                    .Select(x => (int?)x.PlanCalls)
                    .SumAsync(cancellationToken) ?? 0;

                var aiChatSum = await _db.UserAiMonthlyUsages.AsNoTracking()
                    .Where(x => x.PeriodKey == periodKey)
                    .Select(x => (int?)x.ChatCalls)
                    .SumAsync(cancellationToken) ?? 0;

                var aiUsers = await _db.UserAiMonthlyUsages.AsNoTracking()
                    .CountAsync(x => x.PeriodKey == periodKey && (x.PlanCalls > 0 || x.ChatCalls > 0), cancellationToken);

                var activePlans = await _db.PremiumPlanConfigs.AsNoTracking().CountAsync(c => c.IsActive, cancellationToken);

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
                    TotalSystemIncomeVnd = totalSystemIncomeVnd,
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
                    GeneratedAtUtc = utcNow,
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building admin dashboard summary");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Chuỗi dữ liệu user metrics theo ngày (UTC) cho dashboard thống kê admin.
        /// </summary>
        [HttpGet("user-metrics")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserMetrics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var (_, err) = await RequireStaffOrAdminAsync();
                if (err != null) return err;

                var utcNow = DateTime.UtcNow.Date;
                var start = (startDate?.Date ?? utcNow.AddDays(-29));
                var end = (endDate?.Date ?? utcNow);
                if (end < start) (start, end) = (end, start);

                var daySpan = (end - start).TotalDays;
                if (daySpan > 366)
                    return BadRequest("Khoảng thời gian tối đa là 366 ngày.");

                var endExclusive = end.AddDays(1);

                var newUsersMap = await _db.Users.AsNoTracking()
                    .Where(u => u.CreatedAt >= start && u.CreatedAt < endExclusive)
                    .GroupBy(u => u.CreatedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Date, x => x.Count, cancellationToken);

                var activeUsersMap = await _db.Users.AsNoTracking()
                    .Where(u => u.LastLoginAt != null && u.LastLoginAt >= start && u.LastLoginAt < endExclusive)
                    .GroupBy(u => u.LastLoginAt!.Value.Date)
                    .Select(g => new { Date = g.Key, Count = g.Select(x => x.Id).Distinct().Count() })
                    .ToDictionaryAsync(x => x.Date, x => x.Count, cancellationToken);

                var premiumBuyersMap = await _db.PremiumOrders.AsNoTracking()
                    .Where(o => o.Status == "Paid" && o.PaidAt != null && o.PaidAt >= start && o.PaidAt < endExclusive)
                    .GroupBy(o => o.PaidAt!.Value.Date)
                    .Select(g => new { Date = g.Key, Count = g.Select(x => x.UserId).Distinct().Count() })
                    .ToDictionaryAsync(x => x.Date, x => x.Count, cancellationToken);

                var result = new List<UserMetricsPointDto>();
                for (var d = start; d <= end; d = d.AddDays(1))
                {
                    result.Add(new UserMetricsPointDto
                    {
                        Date = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        ActiveUsers = activeUsersMap.TryGetValue(d, out var active) ? active : 0,
                        NewUsers = newUsersMap.TryGetValue(d, out var created) ? created : 0,
                        PremiumBuyers = premiumBuyersMap.TryGetValue(d, out var premium) ? premium : 0,
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building admin user metrics");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
