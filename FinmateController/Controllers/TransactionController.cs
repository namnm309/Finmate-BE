using BLL.DTOs.Request;
using BLL.Services;
using FinmateController.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    [Authorize(AuthenticationSchemes = "Clerk,Basic")]
    public class TransactionController : ControllerBase
    {
        private readonly TransactionService _transactionService;
        private readonly UserService _userService;
        private readonly ILogger<TransactionController> _logger;
        private readonly IHubContext<TransactionHub> _transactionHubContext;

        public TransactionController(
            TransactionService transactionService,
            UserService userService,
            ILogger<TransactionController> logger,
            IHubContext<TransactionHub> transactionHubContext)
        {
            _transactionService = transactionService;
            _userService = userService;
            _logger = logger;
            _transactionHubContext = transactionHubContext;
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
        /// Lấy danh sách giao dịch của user (có filter và pagination)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? transactionTypeId,
            [FromQuery] Guid? categoryId,
            [FromQuery] Guid? moneySourceId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "Invalid user" });
                }

                // Validate pagination
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var result = await _transactionService.GetWithFilterAsync(
                    userId.Value,
                    transactionTypeId,
                    categoryId,
                    moneySourceId,
                    startDate,
                    endDate,
                    page,
                    pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy chi tiết giao dịch
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

                var transaction = await _transactionService.GetByIdAsync(id, userId.Value);
                if (transaction == null)
                {
                    return NotFound(new { error = "Transaction not found" });
                }
                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Tạo giao dịch mới (tự động cập nhật balance của MoneySource)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTransactionDto request)
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

                var transaction = await _transactionService.CreateAsync(userId.Value, request);

                // Notify SignalR clients for this user
                await _transactionHubContext.Clients
                    .Group($"user:{userId.Value}")
                    .SendAsync("TransactionsUpdated", new
                    {
                        transactionId = transaction.Id,
                        action = "created"
                    });

                return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transaction");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Cập nhật giao dịch (rollback balance cũ, apply balance mới)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTransactionDto request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "Invalid user" });
                }

                var transaction = await _transactionService.UpdateAsync(id, userId.Value, request);
                if (transaction == null)
                {
                    return NotFound(new { error = "Transaction not found" });
                }

                // Notify SignalR clients for this user
                await _transactionHubContext.Clients
                    .Group($"user:{userId.Value}")
                    .SendAsync("TransactionsUpdated", new
                    {
                        transactionId = transaction.Id,
                        action = "updated"
                    });

                return Ok(transaction);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Xóa giao dịch (rollback balance)
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

                var success = await _transactionService.DeleteAsync(id, userId.Value);
                if (!success)
                {
                    return NotFound(new { error = "Transaction not found" });
                }

                // Notify SignalR clients for this user
                await _transactionHubContext.Clients
                    .Group($"user:{userId.Value}")
                    .SendAsync("TransactionsUpdated", new
                    {
                        transactionId = id,
                        action = "deleted"
                    });

                return Ok(new { message = "Transaction deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting transaction {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
