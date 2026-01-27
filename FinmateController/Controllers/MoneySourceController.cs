using BLL.DTOs.Request;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/money-sources")]
    [Authorize(AuthenticationSchemes = "Clerk,Basic")]
    public class MoneySourceController : ControllerBase
    {
        private readonly MoneySourceService _moneySourceService;
        private readonly UserService _userService;
        private readonly ILogger<MoneySourceController> _logger;

        public MoneySourceController(
            MoneySourceService moneySourceService,
            UserService userService,
            ILogger<MoneySourceController> logger)
        {
            _moneySourceService = moneySourceService;
            _userService = userService;
            _logger = logger;
        }

        private async Task<Guid?> GetCurrentUserIdAsync()
        {
            // Ưu tiên đọc userId (Guid) từ JWT basic
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("userId")?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            // Fallback: token từ Clerk, map sang user trong DB
            var clerkUserId = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(clerkUserId))
            {
                var clerkUserDto = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
                return clerkUserDto?.Id;
            }

            return null;
        }

        /// <summary>
        /// Lấy danh sách nguồn tiền của user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "Invalid user" });
                }

                var moneySources = await _moneySourceService.GetActiveByUserIdAsync(userId.Value);
                return Ok(moneySources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting money sources");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy danh sách icon MaterialIcons phổ biến cho nguồn tiền
        /// </summary>
        [HttpGet("icons")]
        public IActionResult GetIcons()
        {
            var icons = new List<object>
            {
                new { name = "account-balance-wallet", label = "Ví" },
                new { name = "account-balance", label = "Ngân hàng" },
                new { name = "credit-card", label = "Thẻ tín dụng" },
                new { name = "savings", label = "Tiết kiệm" },
                new { name = "wallet", label = "Ví tiền" },
                new { name = "payments", label = "Thanh toán" },
                new { name = "monetization-on", label = "Tiền" },
                new { name = "attach-money", label = "Đô la" },
                new { name = "euro", label = "Euro" },
                new { name = "currency-bitcoin", label = "Bitcoin" },
                new { name = "currency-yen", label = "Yên" },
                new { name = "currency-pound", label = "Bảng Anh" },
                new { name = "shopping-bag", label = "Mua sắm" },
                new { name = "store", label = "Cửa hàng" },
                new { name = "business", label = "Kinh doanh" },
                new { name = "work", label = "Công việc" },
                new { name = "trending-up", label = "Đầu tư tăng" },
                new { name = "show-chart", label = "Biểu đồ" },
                new { name = "pie-chart", label = "Biểu đồ tròn" },
                new { name = "money", label = "Tiền mặt" },
                new { name = "local-atm", label = "ATM" },
                new { name = "receipt", label = "Hóa đơn" },
                new { name = "redeem", label = "Quà tặng" },
                new { name = "card-giftcard", label = "Thẻ quà tặng" }
            };
            return Ok(icons);
        }

        /// <summary>
        /// Lấy danh sách nguồn tiền đã group theo AccountType (cho màn Account)
        /// </summary>
        [HttpGet("grouped")]
        public async Task<IActionResult> GetGrouped()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "Invalid user" });
                }

                var result = await _moneySourceService.GetGroupedByUserIdAsync(userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grouped money sources");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy chi tiết nguồn tiền
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "Invalid user" });
                }

                var moneySource = await _moneySourceService.GetByIdAsync(id, userId.Value);
                if (moneySource == null)
                {
                    return NotFound(new { error = "Money source not found" });
                }
                return Ok(moneySource);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting money source {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Tạo nguồn tiền mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMoneySourceDto request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "Invalid user" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var moneySource = await _moneySourceService.CreateAsync(userId.Value, request);
                return CreatedAtAction(nameof(GetById), new { id = moneySource.Id }, moneySource);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating money source");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Cập nhật nguồn tiền
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMoneySourceDto request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "Invalid user" });
                }

                var moneySource = await _moneySourceService.UpdateAsync(id, userId.Value, request);
                if (moneySource == null)
                {
                    return NotFound(new { error = "Money source not found" });
                }
                return Ok(moneySource);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating money source {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Xóa nguồn tiền (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "Invalid user" });
                }

                var success = await _moneySourceService.DeleteAsync(id, userId.Value);
                if (!success)
                {
                    return NotFound(new { error = "Money source not found" });
                }
                return Ok(new { message = "Money source deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting money source {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
