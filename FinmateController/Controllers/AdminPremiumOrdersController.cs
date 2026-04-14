using BLL.Services;
using DAL.Data;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> List(
            [FromQuery] string? status,
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

                if (!string.IsNullOrWhiteSpace(q))
                {
                    var qq = q.Trim().ToLowerInvariant();
                    query = query.Where(x =>
                        (x.u.Email != null && x.u.Email.ToLower().Contains(qq)) ||
                        (x.o.PaymentCode != null && x.o.PaymentCode.ToLower().Contains(qq)) ||
                        (x.o.ReferenceCode != null && x.o.ReferenceCode.ToLower().Contains(qq)));
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

