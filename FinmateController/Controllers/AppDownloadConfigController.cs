using BLL.Services;
using DAL.Data;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/app-download-config")]
    public class AppDownloadConfigController : ControllerBase
    {
        private readonly FinmateContext _db;
        private readonly ClerkService _clerkService;
        private readonly UserService _userService;
        private readonly ILogger<AppDownloadConfigController> _logger;

        public AppDownloadConfigController(
            FinmateContext db,
            ClerkService clerkService,
            UserService userService,
            ILogger<AppDownloadConfigController> logger)
        {
            _db = db;
            _clerkService = clerkService;
            _userService = userService;
            _logger = logger;
        }

        public class AppDownloadConfigDto
        {
            public string? IosUrl { get; set; }
            public string? AndroidUrl { get; set; }
            public DateTime UpdatedAt { get; set; }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get()
        {
            var row = await _db.AppDownloadConfigs.AsNoTracking()
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefaultAsync();

            if (row == null)
            {
                return Ok(new AppDownloadConfigDto
                {
                    IosUrl = null,
                    AndroidUrl = null,
                    UpdatedAt = DateTime.UtcNow,
                });
            }

            return Ok(new AppDownloadConfigDto
            {
                IosUrl = row.IosUrl,
                AndroidUrl = row.AndroidUrl,
                UpdatedAt = row.UpdatedAt,
            });
        }

        public class UpsertAppDownloadConfigRequest
        {
            public string? IosUrl { get; set; }
            public string? AndroidUrl { get; set; }
        }

        private static string? NormalizeUrl(string? v)
        {
            var s = (v ?? "").Trim();
            if (string.IsNullOrWhiteSpace(s)) return null;

            if (!Uri.TryCreate(s, UriKind.Absolute, out var uri)) return "__INVALID__";
            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return "__INVALID__";
            return s;
        }

        /// <summary>
        /// Staff/Admin endpoint: cấu hình link tải app (iOS/Android).
        /// </summary>
        [HttpPut]
        [AllowAnonymous]
        public async Task<IActionResult> Upsert([FromBody] UpsertAppDownloadConfigRequest body)
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

                if ((int)me.Role < (int)Role.Staff)
                {
                    return Forbid();
                }

                var ios = NormalizeUrl(body?.IosUrl);
                var android = NormalizeUrl(body?.AndroidUrl);
                if (ios == "__INVALID__") return BadRequest("IosUrl is invalid");
                if (android == "__INVALID__") return BadRequest("AndroidUrl is invalid");

                var now = DateTime.UtcNow;
                var row = await _db.AppDownloadConfigs
                    .OrderByDescending(x => x.UpdatedAt)
                    .FirstOrDefaultAsync();

                if (row == null)
                {
                    row = new AppDownloadConfig
                    {
                        Id = Guid.NewGuid(),
                        CreatedAt = now,
                    };
                    _db.AppDownloadConfigs.Add(row);
                }

                row.IosUrl = ios;
                row.AndroidUrl = android;
                row.UpdatedAt = now;

                await _db.SaveChangesAsync();

                return Ok(new AppDownloadConfigDto
                {
                    IosUrl = row.IosUrl,
                    AndroidUrl = row.AndroidUrl,
                    UpdatedAt = row.UpdatedAt,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting app download config");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

