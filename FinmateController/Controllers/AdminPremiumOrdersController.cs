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
    [Route("api/admin/premium-orders")]
    public class AdminPremiumOrdersController : ControllerBase
    {
        private readonly FinmateContext _db;
        private readonly ClerkService _clerkService;
        private readonly UserService _userService;
        private readonly ILogger<AdminPremiumOrdersController> _logger;

        public AdminPremiumOrdersController(
            FinmateContext db,
            ClerkService clerkService,
            UserService userService,
            ILogger<AdminPremiumOrdersController> logger)
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
            {
                return (null, Unauthorized("Missing Bearer token"));
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var (clerkUserId, err) = await _clerkService.VerifyTokenAndGetUserIdWithErrorAsync(token);
            if (string.IsNullOrWhiteSpace(clerkUserId))
            {
                return (null, Unauthorized(err ?? "Invalid token"));
            }

            var meDto = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
            if (meDto == null) return (null, Unauthorized("User not found"));
            if ((int)meDto.Role < (int)Role.Staff) return (null, Forbid());

            var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == meDto.Id);
            if (me == null) return (null, Unauthorized("User not found"));

            return (me, null);
        }

        public class AdminPremiumOrderRow
        {
            public Guid Id { get; set; }
            public string Plan { get; set; } = string.Empty;
            public decimal AmountVnd { get; set; }
            public string Status { get; set; } = string.Empty;
            public string PaymentCode { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public DateTime? PaidAt { get; set; }

            public string UserEmail { get; set; } = string.Empty;
            public Guid UserId { get; set; }

            public long? SepayTransactionId { get; set; }
            public string? ReferenceCode { get; set; }
        }

        public class PagedResponse<T>
        {
            public List<T> Items { get; set; } = new();
            public int Page { get; set; }
            public int PerPage { get; set; }
            public int Total { get; set; }
            public PremiumOrdersCharts Charts { get; set; } = new();
        }

        public class PremiumOrdersCharts
        {
            public List<OrdersByDayPoint> OrdersByDay { get; set; } = new();
            public List<RevenueByDayPoint> RevenueByDay { get; set; } = new();
            public List<PlanBreakdownPoint> PlanBreakdown { get; set; } = new();
        }

        public class OrdersByDayPoint
        {
            public string Date { get; set; } = string.Empty; // yyyy-MM-dd (UTC)
            public int Count { get; set; }
        }

        public class RevenueByDayPoint
        {
            public string Date { get; set; } = string.Empty; // yyyy-MM-dd (UTC)
            public decimal AmountVnd { get; set; }
        }

        public class PlanBreakdownPoint
        {
            public string Plan { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> List(
            [FromQuery] string? status,
            [FromQuery] string? plan,
            [FromQuery] decimal? minAmountVnd,
            [FromQuery] decimal? maxAmountVnd,
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20)
        {
            try
            {
                var (_, err) = await RequireStaffOrAdminAsync();
                if (err != null) return err;

                page = page < 1 ? 1 : page;
                perPage = perPage < 1 ? 20 : perPage;
                perPage = Math.Min(perPage, 100);

                var query = _db.PremiumOrders.AsNoTracking()
                    .Join(_db.Users.AsNoTracking(),
                        o => o.UserId,
                        u => u.Id,
                        (o, u) => new { o, u });

                if (!string.IsNullOrWhiteSpace(status))
                {
                    var s = status.Trim();
                    query = query.Where(x => x.o.Status == s);
                }

                if (!string.IsNullOrWhiteSpace(plan))
                {
                    var p = plan.Trim();
                    query = query.Where(x => x.o.Plan == p);
                }

                if (minAmountVnd.HasValue && maxAmountVnd.HasValue && minAmountVnd.Value > maxAmountVnd.Value)
                {
                    (minAmountVnd, maxAmountVnd) = (maxAmountVnd, minAmountVnd);
                }

                if (minAmountVnd.HasValue)
                {
                    var min = minAmountVnd.Value;
                    query = query.Where(x => x.o.AmountVnd >= min);
                }

                if (maxAmountVnd.HasValue)
                {
                    var max = maxAmountVnd.Value;
                    query = query.Where(x => x.o.AmountVnd <= max);
                }

                if (!string.IsNullOrWhiteSpace(q))
                {
                    var qq = q.Trim().ToLowerInvariant();
                    query = query.Where(x =>
                        (x.u.Email != null && x.u.Email.ToLower().Contains(qq)) ||
                        (x.o.PaymentCode != null && x.o.PaymentCode.ToLower().Contains(qq)) ||
                        (x.o.ReferenceCode != null && x.o.ReferenceCode.ToLower().Contains(qq)));
                }

                var utcToday = DateTime.UtcNow.Date;
                var chartStartDate = utcToday.AddDays(-6);
                var chartEndExclusive = utcToday.AddDays(1);

                var chartBaseQuery = query.Where(x =>
                    x.o.CreatedAt >= chartStartDate &&
                    x.o.CreatedAt < chartEndExclusive);

                var byDayRaw = await chartBaseQuery
                    .GroupBy(x => x.o.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count(),
                        AmountVnd = g.Sum(x => x.o.AmountVnd),
                    })
                    .ToListAsync();

                var planBreakdownRaw = await chartBaseQuery
                    .GroupBy(x => x.o.Plan)
                    .Select(g => new PlanBreakdownPoint
                    {
                        Plan = g.Key,
                        Count = g.Count(),
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                var byDayMap = byDayRaw.ToDictionary(x => x.Date, x => x, EqualityComparer<DateTime>.Default);
                var ordersByDay = new List<OrdersByDayPoint>(capacity: 7);
                var revenueByDay = new List<RevenueByDayPoint>(capacity: 7);
                for (var i = 0; i < 7; i++)
                {
                    var d = chartStartDate.AddDays(i);
                    var key = d.Date;
                    byDayMap.TryGetValue(key, out var v);

                    var dateStr = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    ordersByDay.Add(new OrdersByDayPoint
                    {
                        Date = dateStr,
                        Count = v?.Count ?? 0,
                    });
                    revenueByDay.Add(new RevenueByDayPoint
                    {
                        Date = dateStr,
                        AmountVnd = v?.AmountVnd ?? 0m,
                    });
                }

                var total = await query.CountAsync();

                var items = await query
                    .OrderByDescending(x => x.o.CreatedAt)
                    .Skip((page - 1) * perPage)
                    .Take(perPage)
                    .Select(x => new AdminPremiumOrderRow
                    {
                        Id = x.o.Id,
                        Plan = x.o.Plan,
                        AmountVnd = x.o.AmountVnd,
                        Status = x.o.Status,
                        PaymentCode = x.o.PaymentCode,
                        CreatedAt = x.o.CreatedAt,
                        ExpiresAt = x.o.ExpiresAt,
                        PaidAt = x.o.PaidAt,
                        UserEmail = x.u.Email,
                        UserId = x.u.Id,
                        SepayTransactionId = x.o.SepayTransactionId,
                        ReferenceCode = x.o.ReferenceCode,
                    })
                    .ToListAsync();

                return Ok(new PagedResponse<AdminPremiumOrderRow>
                {
                    Items = items,
                    Page = page,
                    PerPage = perPage,
                    Total = total,
                    Charts = new PremiumOrdersCharts
                    {
                        OrdersByDay = ordersByDay,
                        RevenueByDay = revenueByDay,
                        PlanBreakdown = planBreakdownRaw,
                    },
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing premium orders");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> Detail([FromRoute] Guid id)
        {
            try
            {
                var (_, err) = await RequireStaffOrAdminAsync();
                if (err != null) return err;

                var item = await _db.PremiumOrders.AsNoTracking()
                    .Join(_db.Users.AsNoTracking(), o => o.UserId, u => u.Id, (o, u) => new { o, u })
                    .Where(x => x.o.Id == id)
                    .Select(x => new AdminPremiumOrderRow
                    {
                        Id = x.o.Id,
                        Plan = x.o.Plan,
                        AmountVnd = x.o.AmountVnd,
                        Status = x.o.Status,
                        PaymentCode = x.o.PaymentCode,
                        CreatedAt = x.o.CreatedAt,
                        ExpiresAt = x.o.ExpiresAt,
                        PaidAt = x.o.PaidAt,
                        UserEmail = x.u.Email,
                        UserId = x.u.Id,
                        SepayTransactionId = x.o.SepayTransactionId,
                        ReferenceCode = x.o.ReferenceCode,
                    })
                    .FirstOrDefaultAsync();

                if (item == null) return NotFound();
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading premium order detail {OrderId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

