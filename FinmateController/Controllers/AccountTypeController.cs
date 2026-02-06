using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/account-types")]
    [Authorize]
    public class AccountTypeController : ControllerBase
    {
        private readonly AccountTypeService _accountTypeService;
        private readonly ILogger<AccountTypeController> _logger;

        public AccountTypeController(
            AccountTypeService accountTypeService,
            ILogger<AccountTypeController> logger)
        {
            _accountTypeService = accountTypeService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách tất cả loại tài khoản (6 loại cố định)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var accountTypes = await _accountTypeService.GetAllAsync();
                return Ok(accountTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account types");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết loại tài khoản
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var accountType = await _accountTypeService.GetByIdAsync(id);
                if (accountType == null)
                {
                    return NotFound(new { error = "Account type not found" });
                }
                return Ok(accountType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account type {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
