using BLL.DTOs.Request;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/categories")]
    [Authorize(AuthenticationSchemes = "Clerk,Basic")]
    public class CategoryController : ControllerBase
    {
        private readonly CategoryService _categoryService;
        private readonly UserService _userService;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(
            CategoryService categoryService,
            UserService userService,
            ILogger<CategoryController> logger)
        {
            _categoryService = categoryService;
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
        /// Lấy danh sách categories của user (tự động seed nếu chưa có)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? transactionTypeId)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "Invalid user" });
                }

                if (transactionTypeId.HasValue)
                {
                    var categories = await _categoryService.GetByTransactionTypeAsync(userId.Value, transactionTypeId.Value);
                    return Ok(categories);
                }
                else
                {
                    var categories = await _categoryService.GetActiveByUserIdAsync(userId.Value);
                    return Ok(categories);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy chi tiết category
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

                var category = await _categoryService.GetByIdAsync(id, userId.Value);
                if (category == null)
                {
                    return NotFound(new { error = "Category not found" });
                }
                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Tạo category mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto request)
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

                var category = await _categoryService.CreateAsync(userId.Value, request);
                return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Cập nhật category
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "Invalid user" });
                }

                var category = await _categoryService.UpdateAsync(id, userId.Value, request);
                if (category == null)
                {
                    return NotFound(new { error = "Category not found" });
                }
                return Ok(category);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Xóa category
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

                var (success, errorMessage) = await _categoryService.DeleteAsync(id, userId.Value);
                if (!success)
                {
                    return NotFound(new { error = errorMessage ?? "Category not found" });
                }
                return Ok(new { message = "Category deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
