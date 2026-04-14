using BLL.Services;
using DAL.Data;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/sepay-gateway")]
    public class SepayGatewayController : ControllerBase
    {
        private readonly FinmateContext _db;
        private readonly ClerkService _clerkService;
        private readonly UserService _userService;
        private readonly IConfiguration _config;
        private readonly ILogger<SepayGatewayController> _logger;

        private static readonly HashSet<string> AllowedPlans = new(StringComparer.OrdinalIgnoreCase)
        {
            "1-month",
            "6-month",
            "1-year",
        };

        public SepayGatewayController(
            FinmateContext db,
            ClerkService clerkService,
            UserService userService,
            IConfiguration config,
            ILogger<SepayGatewayController> logger)
        {
            _db = db;
            _clerkService = clerkService;
            _userService = userService;
            _config = config;
            _logger = logger;
        }

        private string RequireEnv(string name)
        {
            var v = _config[name];
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

        public class InitCheckoutRequest
        {
            public string Plan { get; set; } = string.Empty;
        }

        public class InitCheckoutResponse
        {
            public Guid OrderId { get; set; }
            public string CheckoutUrl { get; set; } = string.Empty;
            public Dictionary<string, string> Fields { get; set; } = new();
        }

        private static string SignFields(Dictionary<string, string> fields, string secretKey)
        {
            // Based on SePay docs: sign a comma-separated list of field=value pairs
            var signedFieldNames = new[]
            {
                "merchant","operation","payment_method","order_amount","currency",
                "order_invoice_number","order_description","customer_id",
                "success_url","error_url","cancel_url"
            };

            var parts = new List<string>(signedFieldNames.Length);
            foreach (var name in signedFieldNames)
            {
                if (!fields.TryGetValue(name, out var value)) continue;
                parts.Add($"{name}={value ?? ""}");
            }

            var data = string.Join(",", parts);
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }

        [HttpPost("init")]
        [AllowAnonymous]
        public async Task<IActionResult> Init([FromBody] InitCheckoutRequest body)
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
                if (cfg == null) return BadRequest("Plan is not available");

                var now = DateTime.UtcNow;
                var orderId = Guid.NewGuid();
                var invoice = $"FM_{orderId:N}".ToUpperInvariant();

                var expiresAt = now.AddMinutes(30);
                var order = new PremiumOrder
                {
                    Id = orderId,
                    UserId = me.Id,
                    Plan = body.Plan,
                    AmountVnd = cfg.PriceVnd,
                    PaymentCode = invoice,
                    Status = "Pending",
                    ExpiresAt = expiresAt,
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                _db.PremiumOrders.Add(order);
                await _db.SaveChangesAsync();

                // SePay payment gateway production checkout endpoint
                var checkoutUrl = "https://pay.sepay.vn/v1/checkout/init";

                var merchantId = RequireEnv("SEPAY_PG_MERCHANT_ID");
                var secretKey = RequireEnv("SEPAY_PG_SECRET_KEY");
                var feBaseUrl = RequireEnv("FINMATE_FE_BASE_URL").TrimEnd('/');

                // Return/callback pages on FE; FE will poll BE for actual status.
                var successUrl = $"{feBaseUrl}/sepay-return?orderId={orderId}&result=success";
                var errorUrl = $"{feBaseUrl}/sepay-return?orderId={orderId}&result=error";
                var cancelUrl = $"{feBaseUrl}/sepay-return?orderId={orderId}&result=cancel";

                var fields = new Dictionary<string, string>
                {
                    ["merchant"] = merchantId,
                    ["currency"] = "VND",
                    ["operation"] = "PURCHASE",
                    ["payment_method"] = "BANK_TRANSFER",
                    ["order_amount"] = ((long)cfg.PriceVnd).ToString(CultureInfo.InvariantCulture),
                    ["order_invoice_number"] = invoice,
                    ["order_description"] = $"Nang cap tai khoan ({body.Plan})",
                    ["customer_id"] = me.Id.ToString(),
                    ["success_url"] = successUrl,
                    ["error_url"] = errorUrl,
                    ["cancel_url"] = cancelUrl,
                };

                fields["signature"] = SignFields(fields, secretKey);

                return Ok(new InitCheckoutResponse
                {
                    OrderId = orderId,
                    CheckoutUrl = checkoutUrl,
                    Fields = fields,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error init SePay gateway checkout");
                return StatusCode(500, "Internal server error");
            }
        }

        public class SepayIpnPayload
        {
            public long Timestamp { get; set; }
            public string? NotificationType { get; set; }
            public SepayIpnOrder? Order { get; set; }
            public SepayIpnTransaction? Transaction { get; set; }
        }

        public class SepayIpnOrder
        {
            public string? OrderInvoiceNumber { get; set; }
            public string? OrderAmount { get; set; }
            public string? OrderStatus { get; set; }
        }

        public class SepayIpnTransaction
        {
            public string? TransactionId { get; set; }
            public string? TransactionStatus { get; set; }
            public string? TransactionAmount { get; set; }
            public string? TransactionDate { get; set; }
            public string? PaymentMethod { get; set; }
        }

        [HttpPost("ipn")]
        [AllowAnonymous]
        public async Task<IActionResult> Ipn()
        {
            string raw = "";
            try
            {
                // If merchant configured auth type = SECRET_KEY, SePay sends X-Secret-Key header
                var expected = RequireEnv("SEPAY_PG_SECRET_KEY");
                var got = Request.Headers["X-Secret-Key"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(got) && !string.Equals(got.Trim(), expected, StringComparison.Ordinal))
                {
                    return Unauthorized(new { error = "Unauthorized" });
                }

                Request.EnableBuffering();
                raw = await new StreamReader(Request.Body).ReadToEndAsync();
                Request.Body.Position = 0;

                var payload = JsonSerializer.Deserialize<SepayIpnPayload>(raw, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                });

                if (payload?.Order?.OrderInvoiceNumber == null)
                {
                    return BadRequest(new { error = "Invalid payload" });
                }

                if (!string.Equals(payload.NotificationType, "ORDER_PAID", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new { success = true, ignored = "notification_type" });
                }

                var invoice = payload.Order.OrderInvoiceNumber.Trim().ToUpperInvariant();
                var order = await _db.PremiumOrders.FirstOrDefaultAsync(o => o.PaymentCode == invoice);
                if (order == null) return Ok(new { success = true, ignored = "order_not_found" });

                if (string.Equals(order.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new { success = true, dedup = true });
                }

                var now = DateTime.UtcNow;
                if (now > order.ExpiresAt)
                {
                    order.Status = "Expired";
                    order.UpdatedAt = now;
                    await _db.SaveChangesAsync();
                    return Ok(new { success = true, ignored = "order_expired" });
                }

                // Verify amount
                if (decimal.TryParse(payload.Order.OrderAmount, NumberStyles.Any, CultureInfo.InvariantCulture, out var paidAmount))
                {
                    if (paidAmount != order.AmountVnd)
                    {
                        _logger.LogWarning("IPN amount mismatch. OrderId={OrderId} Expect={Expect} Got={Got}", order.Id, order.AmountVnd, paidAmount);
                        return Ok(new { success = true, ignored = "amount_mismatch" });
                    }
                }

                await using var tx = await _db.Database.BeginTransactionAsync();

                order.Status = "Paid";
                order.PaidAt = now;
                order.UpdatedAt = now;
                order.ReferenceCode = payload.Transaction?.TransactionId;
                order.LastWebhookContent = payload.Transaction?.TransactionStatus;

                // deactivate existing active subs
                var activeSubs = await _db.PremiumSubscriptions
                    .Where(s => s.UserId == order.UserId && s.IsActive)
                    .OrderByDescending(s => s.ExpiresAt)
                    .ToListAsync();

                var baseStart = now;
                var latestActiveExpiry = activeSubs.FirstOrDefault()?.ExpiresAt;
                if (latestActiveExpiry.HasValue && latestActiveExpiry.Value > baseStart)
                    baseStart = latestActiveExpiry.Value;

                var expiresAt = order.Plan switch
                {
                    "1-month" => baseStart.AddMonths(1),
                    "6-month" => baseStart.AddMonths(6),
                    "1-year" => baseStart.AddYears(1),
                    _ => baseStart.AddMonths(1),
                };

                foreach (var s in activeSubs)
                {
                    s.IsActive = false;
                    s.UpdatedAt = now;
                }

                _db.PremiumSubscriptions.Add(new PremiumSubscription
                {
                    Id = Guid.NewGuid(),
                    UserId = order.UserId,
                    Plan = order.Plan,
                    PurchasedAt = now,
                    ExpiresAt = expiresAt,
                    IsActive = true,
                    PaymentMethod = "SEPAY_GATEWAY",
                    TransactionId = payload.Transaction?.TransactionId,
                    CreatedAt = now,
                    UpdatedAt = now,
                });

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == order.UserId);
                if (user != null)
                {
                    user.IsPremium = true;
                    user.UpdatedAt = now;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling SePay IPN. RawLen={Len}", raw?.Length ?? 0);
                return Ok(new { success = true });
            }
        }
    }
}

