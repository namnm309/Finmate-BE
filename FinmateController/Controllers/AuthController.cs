using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BLL.Services;
using BLL.DTOs.Request;
using BLL.DTOs.Response;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ClerkService _clerkService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserService userService,
            ClerkService clerkService,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _clerkService = clerkService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy thông tin user hiện tại từ JWT token
        /// </summary>
        [HttpGet("me")]
        [Authorize]
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
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Verify token và lấy thông tin từ Clerk (không cần database)
        /// </summary>
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyToken([FromBody] VerifyTokenRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Token))
                {
                    return BadRequest("Token is required");
                }

                var clerkUser = await _clerkService.VerifyTokenAndGetUserAsync(request.Token);
                if (clerkUser == null)
                {
                    return Unauthorized("Invalid token");
                }

                var response = new ClerkUserResponseDto
                {
                    Id = clerkUser.Id,
                    Email = clerkUser.EmailAddress,
                    FirstName = clerkUser.FirstName,
                    LastName = clerkUser.LastName,
                    PhoneNumber = clerkUser.PhoneNumber,
                    ImageUrl = clerkUser.ImageUrl,
                    CreatedAt = clerkUser.CreatedAt,
                    UpdatedAt = clerkUser.UpdatedAt,
                    LastSignInAt = clerkUser.LastSignInAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying token");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Test endpoint để kiểm tra authentication
        /// </summary>
        [HttpGet("test")]
        [Authorize]
        public IActionResult TestAuth()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Ok(new
            {
                message = "Authentication successful",
                claims = claims,
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value
            });
        }
    }
}
