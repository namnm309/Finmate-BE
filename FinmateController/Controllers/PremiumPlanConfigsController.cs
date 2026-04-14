using BLL.Services;
using DAL.Data;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/premium-plan-configs")]
    public class PremiumPlanConfigsController : ControllerBase
    {
        private readonly FinmateContext _db;
        private readonly ClerkService _clerkService;
        private readonly UserService _userService;
        private readonly ILogger<PremiumPlanConfigsController> _logger;

        private static readonly HashSet<string> AllowedPlans = new(StringComparer.OrdinalIgnoreCase)
        {
            "1-month",
            "6-month",
            "1-year",
        };

        public PremiumPlanConfigsController(
            FinmateContext db,
            ClerkService clerkService,
            UserService userService,
            ILogger<PremiumPlanConfigsController> logger)
        {
            _db = db;
            _clerkService = clerkService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Public endpoint cho landing page: lấy danh sách gói premium đang active.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetActive()
        {
            var items = await _db.PremiumPlanConfigs
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Plan)
                .ToListAsync();

            return Ok(items);
        }

        public class UpsertPremiumPlanConfigRequest
        {
            public string Plan { get; set; } = string.Empty;
            public decimal PriceVnd { get; set; }
            public decimal? OriginalPriceVnd { get; set; }
            public int? DiscountPercent { get; set; }
            public bool IsActive { get; set; } = true;
        }

        /// <summary>
        /// Staff/Admin endpoint: cập nhật cấu hình giá gói premium.
        /// Xác thực token theo cùng cơ chế /api/auth/me-external (verify qua ClerkService).
        /// </summary>
        [HttpPut]
        [AllowAnonymous]
        public async Task<IActionResult> Upsert([FromBody] List<UpsertPremiumPlanConfigRequest> body)
        {
            try
            {
                var authHeader = Request.Headers.Authorization.ToString();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return Unauthorized("Missing Bearer token");
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                var (clerkUserId, error) = await _clerkService.VerifyTokenAndGetUserIdWithErrorAsync(token);
                if (string.IsNullOrWhiteSpace(clerkUserId))
                {
                    return Unauthorized(error ?? "Invalid token");
                }

                var me = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
                if (me == null)
                {
                    return Unauthorized("User not found");
                }

                var roleNum = (int)me.Role;
                if (roleNum < (int)Role.Staff)
                {
                    return Forbid();
                }

                if (body == null || body.Count == 0)
                {
                    return BadRequest("Empty body");
                }

                foreach (var item in body)
                {
                    if (string.IsNullOrWhiteSpace(item.Plan) || !AllowedPlans.Contains(item.Plan))
                    {
                        return BadRequest($"Invalid plan: {item.Plan}");
                    }

                    if (item.PriceVnd < 0)
                    {
                        return BadRequest($"PriceVnd must be >= 0 for plan {item.Plan}");
                    }

                    if (item.OriginalPriceVnd is not null && item.OriginalPriceVnd < 0)
                    {
                        return BadRequest($"OriginalPriceVnd must be >= 0 for plan {item.Plan}");
                    }

                    if (item.DiscountPercent is not null && (item.DiscountPercent < 0 || item.DiscountPercent > 100))
                    {
                        return BadRequest($"DiscountPercent must be 0..100 for plan {item.Plan}");
                    }
                }

                var now = DateTime.UtcNow;
                var plans = body.Select(x => x.Plan).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var existing = await _db.PremiumPlanConfigs
                    .Where(x => plans.Contains(x.Plan))
                    .ToListAsync();

                foreach (var req in body)
                {
                    var row = existing.FirstOrDefault(x => string.Equals(x.Plan, req.Plan, StringComparison.OrdinalIgnoreCase));
                    if (row == null)
                    {
                        row = new PremiumPlanConfig
                        {
                            Id = Guid.NewGuid(),
                            Plan = req.Plan,
                            CreatedAt = now,
                        };
                        _db.PremiumPlanConfigs.Add(row);
                    }

                    row.PriceVnd = req.PriceVnd;
                    row.OriginalPriceVnd = req.OriginalPriceVnd;
                    row.DiscountPercent = req.DiscountPercent;
                    row.IsActive = req.IsActive;
                    row.UpdatedAt = now;
                }

                await _db.SaveChangesAsync();

                var outItems = await _db.PremiumPlanConfigs
                    .AsNoTracking()
                    .Where(x => AllowedPlans.Contains(x.Plan))
                    .OrderBy(x => x.Plan)
                    .ToListAsync();

                return Ok(outItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting premium plan configs");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

