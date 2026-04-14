using BLL.Services;
using DAL.Data;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/premium-orders")]
    public class PremiumOrdersController : ControllerBase
    {
        private readonly FinmateContext _db;
        private readonly ClerkService _clerkService;
        private readonly UserService _userService;
        private readonly IConfiguration _config;
        private readonly ILogger<PremiumOrdersController> _logger;

        private static readonly HashSet<string> AllowedPlans = new(StringComparer.OrdinalIgnoreCase)
        {
            "1-month",
            "6-month",
            "1-year",
        };

        public PremiumOrdersController(
            FinmateContext db,
            ClerkService clerkService,
            UserService userService,
            IConfiguration config,
            ILogger<PremiumOrdersController> logger)
        {
            _db = db;
            _clerkService = clerkService;
            _userService = userService;
            _config = config;
            _logger = logger;
        }

        public class CreatePremiumOrderRequest
        {
            public string Plan { get; set; } = string.Empty;
        }

        public class PremiumOrderCheckoutDto
        {
            public Guid Id { get; set; }
            public string Plan { get; set; } = string.Empty;
            public decimal AmountVnd { get; set; }
            public string Status { get; set; } = string.Empty;
            public string PaymentCode { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public DateTime? PaidAt { get; set; }

            public string? QrBank { get; set; }
            public string? QrAccountNumber { get; set; }
        }

        private static string RequireEnv(IConfiguration config, string name)
        {
            var v = config[name];
            if (string.IsNullOrWhiteSpace(v)) throw new InvalidOperationException($"{name} is not set");
            return v.Trim();
        }

        private async Task<(string? clerkUserId, IActionResult? error)> RequireClerkUserIdFromBearerAsync()
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

            return (clerkUserId, null);
        }

        private static string ToBase32CrockfordNoPadding(byte[] data)
        {
            const string alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
            if (data.Length == 0) return string.Empty;

            var sb = new StringBuilder((int)Math.Ceiling(data.Length * 8 / 5.0));
            int buffer = data[0];
            int next = 1;
            int bitsLeft = 8;
            while (bitsLeft > 0 || next < data.Length)
            {
                if (bitsLeft < 5)
                {
                    if (next < data.Length)
                    {
                        buffer <<= 8;
                        buffer |= (data[next++] & 0xff);
                        bitsLeft += 8;
                    }
                    else
                    {
                        int pad = 5 - bitsLeft;
                        buffer <<= pad;
                        bitsLeft += pad;
                    }
                }

                int index = (buffer >> (bitsLeft - 5)) & 0x1f;
                bitsLeft -= 5;
                sb.Append(alphabet[index]);
            }

            return sb.ToString();
        }

        private static string BuildPaymentCode(Guid orderId)
        {
            // Prefix "FM" + base32(Guid bytes) => chỉ chữ/số, không dấu, phù hợp des của qr.sepay.vn
            var bytes = orderId.ToByteArray();
            return "FM" + ToBase32CrockfordNoPadding(bytes);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] CreatePremiumOrderRequest body)
        {
            try
            {
                var (clerkUserId, authError) = await RequireClerkUserIdFromBearerAsync();
                if (authError != null) return authError;

                if (body == null || string.IsNullOrWhiteSpace(body.Plan) || !AllowedPlans.Contains(body.Plan))
                {
                    return BadRequest("Invalid plan");
                }

                var me = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId!);
                if (me == null) return Unauthorized("User not found");

                var cfg = await _db.PremiumPlanConfigs.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IsActive && x.Plan == body.Plan);
                if (cfg == null)
                {
                    return BadRequest("Plan is not available");
                }

                var now = DateTime.UtcNow;
                var orderId = Guid.NewGuid();
                var paymentCode = BuildPaymentCode(orderId);

                var expiresAt = now.AddMinutes(30);

                var order = new PremiumOrder
                {
                    Id = orderId,
                    UserId = me.Id,
                    Plan = body.Plan,
                    AmountVnd = cfg.PriceVnd,
                    PaymentCode = paymentCode,
                    Status = "Pending",
                    ExpiresAt = expiresAt,
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                _db.PremiumOrders.Add(order);
                await _db.SaveChangesAsync();

                // These are used to generate QR: https://qr.sepay.vn/img?acc=...&bank=...&amount=...&des=...
                // Even if you configured payee in SePay dashboard, FE still needs acc/bank to render QR.
                var qrBank = RequireEnv(_config, "SEPAY_QR_BANK");
                var qrAcc = RequireEnv(_config, "SEPAY_QR_ACC");

                return Ok(new PremiumOrderCheckoutDto
                {
                    Id = order.Id,
                    Plan = order.Plan,
                    AmountVnd = order.AmountVnd,
                    Status = order.Status,
                    PaymentCode = order.PaymentCode,
                    CreatedAt = order.CreatedAt,
                    ExpiresAt = order.ExpiresAt,
                    PaidAt = order.PaidAt,
                    QrBank = qrBank,
                    QrAccountNumber = qrAcc,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating premium order");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            try
            {
                var (clerkUserId, authError) = await RequireClerkUserIdFromBearerAsync();
                if (authError != null) return authError;

                var me = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId!);
                if (me == null) return Unauthorized("User not found");

                var order = await _db.PremiumOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (order == null) return NotFound();

                var isStaffOrAdmin = (int)me.Role >= (int)Role.Staff;
                if (!isStaffOrAdmin && order.UserId != me.Id)
                {
                    return Forbid();
                }

                var qrBank = _config["SEPAY_QR_BANK"]?.Trim();
                var qrAcc = _config["SEPAY_QR_ACC"]?.Trim();

                return Ok(new PremiumOrderCheckoutDto
                {
                    Id = order.Id,
                    Plan = order.Plan,
                    AmountVnd = order.AmountVnd,
                    Status = order.Status,
                    PaymentCode = order.PaymentCode,
                    CreatedAt = order.CreatedAt,
                    ExpiresAt = order.ExpiresAt,
                    PaidAt = order.PaidAt,
                    QrBank = qrBank,
                    QrAccountNumber = qrAcc,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading premium order {OrderId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

