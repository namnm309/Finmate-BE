using BLL.DTOs.Request;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/savings-books")]
    [Authorize(AuthenticationSchemes = "Clerk")]
    public class SavingsBookController : FinmateControllerBase
    {
        private readonly SavingsBookService _savingsBookService;
        private readonly ILogger<SavingsBookController> _logger;

        public SavingsBookController(
            SavingsBookService savingsBookService,
            UserService userService,
            ILogger<SavingsBookController> logger)
            : base(userService)
        {
            _savingsBookService = savingsBookService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách sổ tiết kiệm của user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                    return Unauthorized(new { error = "Invalid user" });

                var list = await _savingsBookService.GetByUserIdAsync(userId.Value);
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting savings books");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết sổ tiết kiệm
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                    return Unauthorized(new { error = "Invalid user" });

                var item = await _savingsBookService.GetByIdAsync(id, userId.Value);
                if (item == null)
                    return NotFound(new { error = "Savings book not found" });
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting savings book {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Tạo sổ tiết kiệm mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSavingsBookDto request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                    return Unauthorized(new { error = "Invalid user" });

                var created = await _savingsBookService.CreateAsync(userId.Value, request);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating savings book");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật sổ tiết kiệm (chỉ khi Status = Active)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSavingsBookDto request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                    return Unauthorized(new { error = "Invalid user" });

                var updated = await _savingsBookService.UpdateAsync(id, userId.Value, request);
                if (updated == null)
                    return NotFound(new { error = "Savings book not found" });
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating savings book {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Xóa sổ tiết kiệm
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                    return Unauthorized(new { error = "Invalid user" });

                var deleted = await _savingsBookService.DeleteAsync(id, userId.Value);
                if (!deleted)
                    return NotFound(new { error = "Savings book not found" });
                return Ok(new { message = "Deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting savings book {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gửi thêm tiền vào sổ tiết kiệm
        /// </summary>
        [HttpPost("{id}/deposit")]
        public async Task<IActionResult> Deposit(Guid id, [FromBody] DepositSavingsBookDto request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                    return Unauthorized(new { error = "Invalid user" });

                var result = await _savingsBookService.DepositAsync(id, userId.Value, request);
                if (result == null)
                    return NotFound(new { error = "Savings book not found" });
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error depositing to savings book {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Rút một phần từ sổ tiết kiệm
        /// </summary>
        [HttpPost("{id}/withdraw")]
        public async Task<IActionResult> Withdraw(Guid id, [FromBody] WithdrawSavingsBookDto request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                    return Unauthorized(new { error = "Invalid user" });

                var result = await _savingsBookService.WithdrawAsync(id, userId.Value, request);
                if (result == null)
                    return NotFound(new { error = "Savings book not found" });
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing from savings book {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Tất toán sổ tiết kiệm - thu tiền vào MoneySource
        /// </summary>
        [HttpPost("{id}/settle")]
        public async Task<IActionResult> Settle(Guid id, [FromBody] SettleSavingsBookDto request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                    return Unauthorized(new { error = "Invalid user" });

                var result = await _savingsBookService.SettleAsync(id, userId.Value, request);
                if (result == null)
                    return NotFound(new { error = "Savings book not found" });
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error settling savings book {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
