using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/transaction-types")]
    [Authorize]
    public class TransactionTypeController : ControllerBase
    {
        private readonly TransactionTypeService _transactionTypeService;
        private readonly ILogger<TransactionTypeController> _logger;

        public TransactionTypeController(TransactionTypeService transactionTypeService, ILogger<TransactionTypeController> logger)
        {
            _transactionTypeService = transactionTypeService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách tất cả loại giao dịch (4 loại cố định: Chi tiêu, Thu tiền, Cho vay, Đi vay)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var transactionTypes = await _transactionTypeService.GetAllAsync();
                return Ok(transactionTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction types");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy chi tiết loại giao dịch
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var transactionType = await _transactionTypeService.GetByIdAsync(id);
                if (transactionType == null)
                {
                    return NotFound(new { error = "Transaction type not found" });
                }
                return Ok(transactionType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction type {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
