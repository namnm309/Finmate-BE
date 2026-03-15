using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/banks")]
    [Authorize(AuthenticationSchemes = "Clerk")]
    public class BankController : ControllerBase
    {
        private readonly BankService _bankService;
        private readonly ILogger<BankController> _logger;

        public BankController(BankService bankService, ILogger<BankController> logger)
        {
            _bankService = bankService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách ngân hàng Việt Nam
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var banks = await _bankService.GetAllAsync();
                return Ok(banks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting banks. Message: {Message}", ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết ngân hàng
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var bank = await _bankService.GetByIdAsync(id);
                if (bank == null)
                    return NotFound(new { error = "Bank not found" });
                return Ok(bank);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bank {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
