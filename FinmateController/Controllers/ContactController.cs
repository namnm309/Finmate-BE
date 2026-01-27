using BLL.DTOs.Request;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/contacts")]
    [Authorize]
    public class ContactController : ControllerBase
    {
        private readonly ContactService _contactService;
        private readonly UserService _userService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(
            ContactService contactService,
            UserService userService,
            ILogger<ContactController> logger)
        {
            _contactService = contactService;
            _userService = userService;
            _logger = logger;
        }

        private async Task<Guid?> GetCurrentUserIdAsync()
        {
            var clerkUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(clerkUserId))
            {
                return null;
            }

            var user = await _userService.GetUserByClerkIdAsync(clerkUserId);
            return user?.Id;
        }

        /// <summary>
        /// Lấy danh sách contacts của user
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

                var contacts = await _contactService.GetActiveByUserIdAsync(userId.Value);
                return Ok(contacts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contacts");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy chi tiết contact
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

                var contact = await _contactService.GetByIdAsync(id, userId.Value);
                if (contact == null)
                {
                    return NotFound(new { error = "Contact not found" });
                }
                return Ok(contact);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contact {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Tạo contact mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateContactDto request)
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

                var contact = await _contactService.CreateAsync(userId.Value, request);
                return CreatedAtAction(nameof(GetById), new { id = contact.Id }, contact);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Cập nhật contact
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContactDto request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "Invalid user" });
                }

                var contact = await _contactService.UpdateAsync(id, userId.Value, request);
                if (contact == null)
                {
                    return NotFound(new { error = "Contact not found" });
                }
                return Ok(contact);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Xóa contact
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

                var (success, errorMessage) = await _contactService.DeleteAsync(id, userId.Value);
                if (!success)
                {
                    return NotFound(new { error = errorMessage ?? "Contact not found" });
                }
                return Ok(new { message = "Contact deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
