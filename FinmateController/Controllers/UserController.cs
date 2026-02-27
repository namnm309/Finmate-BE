using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BLL.Services;
using BLL.DTOs.Request;
using BLL.DTOs.Response;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(AuthenticationSchemes = "Clerk,Basic")]

    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            UserService userService,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy thông tin user hiện tại
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                // Lấy Clerk user ID từ claims
                var clerkUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(clerkUserId))
                {
                    return Unauthorized("Invalid token");
                }

                // Lấy hoặc tạo user từ BLL
                var userDto = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
                
                if (userDto == null)
                {
                    return NotFound("User not found");
                }

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                // Trả về chi tiết lỗi để debug
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Cập nhật thông tin user
        /// </summary>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequestDto request)
        {
            try
            {
                // Lấy Clerk user ID từ claims
                var clerkUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(clerkUserId))
                {
                    return Unauthorized("Invalid token");
                }

                // Validate request
                if (request == null)
                {
                    return BadRequest("Request body is required");
                }

                // Cập nhật user
                var userDto = await _userService.UpdateUserProfileAsync(clerkUserId, request);
                
                if (userDto == null)
                {
                    return NotFound("User not found");
                }

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Xóa tất cả dữ liệu của user
        /// </summary>
        [HttpDelete("me/data")]
        public async Task<IActionResult> DeleteUserData()
        {
            try
            {
                // Lấy Clerk user ID từ claims
                var clerkUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(clerkUserId))
                {
                    return Unauthorized("Invalid token");
                }

                // Xóa dữ liệu
                var success = await _userService.DeleteUserDataAsync(clerkUserId);
                
                if (!success)
                {
                    return NotFound("User not found");
                }

                return Ok(new { message = "All user data has been deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user data");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Xóa tài khoản (soft delete)
        /// </summary>
        [HttpDelete("me")]
        public async Task<IActionResult> DeleteUserAccount()
        {
            try
            {
                // Lấy Clerk user ID từ claims
                var clerkUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(clerkUserId))
                {
                    return Unauthorized("Invalid token");
                }

                // Xóa tài khoản
                var success = await _userService.DeleteUserAccountAsync(clerkUserId);
                
                if (!success)
                {
                    return NotFound("User not found");
                }

                return Ok(new { message = "Account has been deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user account");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
