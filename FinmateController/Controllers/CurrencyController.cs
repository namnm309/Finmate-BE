using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/currencies")]
    [Authorize]
    public class CurrencyController : ControllerBase
    {
        private readonly CurrencyService _currencyService;
        private readonly ILogger<CurrencyController> _logger;

        public CurrencyController(
            CurrencyService currencyService,
            ILogger<CurrencyController> logger)
        {
            _currencyService = currencyService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách tất cả loại tiền tệ
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var currencies = await _currencyService.GetAllAsync();
                return Ok(currencies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting currencies");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết loại tiền tệ theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var currency = await _currencyService.GetByIdAsync(id);
                if (currency == null)
                {
                    return NotFound(new { error = "Currency not found" });
                }
                return Ok(currency);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting currency {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết loại tiền tệ theo code (VND, USD...)
        /// </summary>
        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            try
            {
                var currency = await _currencyService.GetByCodeAsync(code.ToUpper());
                if (currency == null)
                {
                    return NotFound(new { error = "Currency not found" });
                }
                return Ok(currency);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting currency by code {Code}", code);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
