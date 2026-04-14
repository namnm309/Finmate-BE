using DAL.Data;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/sepay")]
    public class SepayWebhookController : ControllerBase
    {
        private readonly FinmateContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<SepayWebhookController> _logger;

        private static readonly Regex PaymentCodeRegex = new(@"\bFM[A-Z0-9]{10,}\b", RegexOptions.Compiled);

        public SepayWebhookController(FinmateContext db, IConfiguration config, ILogger<SepayWebhookController> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        private string RequireEnv(string name)
        {
            var v = _config[name];
            if (string.IsNullOrWhiteSpace(v)) throw new InvalidOperationException($"{name} is not set");
            return v.Trim();
        }

        private bool VerifyApiKey()
        {
            var expected = RequireEnv("SEPAY_WEBHOOK_API_KEY");
            var auth = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(auth)) return false;

            // SePay sends: Authorization: Apikey API_KEY_CUA_BAN
            if (!auth.StartsWith("Apikey ", StringComparison.OrdinalIgnoreCase)) return false;
            var got = auth.Substring("Apikey ".Length).Trim();
            return string.Equals(got, expected, StringComparison.Ordinal);
        }

        public class SepayWebhookPayload
        {
            public long Id { get; set; }
            public string? Gateway { get; set; }
            public string? TransactionDate { get; set; }
            public string? AccountNumber { get; set; }
            public string? Code { get; set; }
            public string? Content { get; set; }
            public string? TransferType { get; set; }
            public decimal TransferAmount { get; set; }
            public decimal Accumulated { get; set; }
            public string? SubAccount { get; set; }
            public string? ReferenceCode { get; set; }
            public string? Description { get; set; }
        }

        private static DateTime? ParseSepayTransactionDateToUtc(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParseExact(s.Trim(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
            return null;
        }

        private static string? ExtractPaymentCode(SepayWebhookPayload payload)
        {
            if (!string.IsNullOrWhiteSpace(payload.Code))
                return payload.Code.Trim().ToUpperInvariant();

            var hay = (payload.Content ?? "") + "\n" + (payload.Description ?? "");
            var m = PaymentCodeRegex.Match(hay.ToUpperInvariant());
            return m.Success ? m.Value : null;
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            string raw = "";
            try
            {
                if (!VerifyApiKey())
                {
                    return Unauthorized(new { error = "Unauthorized" });
                }

                Request.EnableBuffering();
                raw = await new StreamReader(Request.Body).ReadToEndAsync();
                Request.Body.Position = 0;

                var payload = JsonSerializer.Deserialize<SepayWebhookPayload>(raw, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                });

                if (payload == null || payload.Id <= 0)
                {
                    return BadRequest(new { error = "Invalid payload" });
                }

                // Idempotency: store webhook event by sepay.id (unique)
                var ev = new SepayWebhookEvent
                {
                    Id = Guid.NewGuid(),
                    SepayId = payload.Id,
                    Code = payload.Code,
                    Content = payload.Content,
                    TransferType = (payload.TransferType ?? "").Trim(),
                    TransferAmount = payload.TransferAmount,
                    ReferenceCode = payload.ReferenceCode,
                    TransactionDate = ParseSepayTransactionDateToUtc(payload.TransactionDate),
                    RawPayload = raw,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                _db.SepayWebhookEvents.Add(ev);
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Duplicate sepay.id => treat as success
                    return Ok(new { success = true, dedup = true });
                }

                if (!string.Equals(payload.TransferType, "in", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new { success = true, ignored = "transferType!=in" });
                }

                var paymentCode = ExtractPaymentCode(payload);
                if (string.IsNullOrWhiteSpace(paymentCode))
                {
                    _logger.LogWarning("Sepay webhook missing payment code. SepayId={SepayId} Content={Content}", payload.Id, payload.Content);
                    return Ok(new { success = true, ignored = "missing_payment_code" });
                }

                var now = DateTime.UtcNow;

                await using var tx = await _db.Database.BeginTransactionAsync();

                var order = await _db.PremiumOrders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.PaymentCode == paymentCode);

                if (order == null)
                {
                    await tx.CommitAsync();
                    return Ok(new { success = true, ignored = "order_not_found" });
                }

                if (!string.Equals(order.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    await tx.CommitAsync();
                    return Ok(new { success = true, ignored = "order_not_pending" });
                }

                if (now > order.ExpiresAt)
                {
                    order.Status = "Expired";
                    order.UpdatedAt = now;
                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                    return Ok(new { success = true, ignored = "order_expired" });
                }

                if (payload.TransferAmount != order.AmountVnd)
                {
                    _logger.LogWarning("Sepay amount mismatch. OrderId={OrderId} Expect={Expect} Got={Got}", order.Id, order.AmountVnd, payload.TransferAmount);
                    await tx.CommitAsync();
                    return Ok(new { success = true, ignored = "amount_mismatch" });
                }

                order.Status = "Paid";
                order.PaidAt = now;
                order.SepayTransactionId = payload.Id;
                order.ReferenceCode = payload.ReferenceCode;
                order.BankGateway = payload.Gateway;
                order.AccountNumber = payload.AccountNumber;
                order.LastWebhookContent = payload.Content;
                order.UpdatedAt = now;

                // Activate/extend subscription
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

                var sub = new PremiumSubscription
                {
                    Id = Guid.NewGuid(),
                    UserId = order.UserId,
                    Plan = order.Plan,
                    PurchasedAt = now,
                    ExpiresAt = expiresAt,
                    IsActive = true,
                    PaymentMethod = "SEPAY_BANK_TRANSFER",
                    TransactionId = payload.Id.ToString(CultureInfo.InvariantCulture),
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                _db.PremiumSubscriptions.Add(sub);

                // Set user premium flag
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == order.UserId);
                if (user != null)
                {
                    user.IsPremium = true;
                    user.UpdatedAt = now;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // SePay expects {"success": true} with 200/201 for API Key auth.
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Sepay webhook. RawLen={Len}", raw?.Length ?? 0);
                // Return 200 to avoid infinite retry storms; still log on our side.
                return Ok(new { success = true, error = "handled" });
            }
        }
    }
}

