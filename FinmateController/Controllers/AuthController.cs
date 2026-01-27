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
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserService userService,
            ClerkService clerkService,
            AuthService authService,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _clerkService = clerkService;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Đăng ký tài khoản mới với username/password
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userDto = await _authService.RegisterAsync(request);
                return Ok(userDto);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Đăng nhập với email/password và nhận JWT token
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var loginResponse = await _authService.LoginAsync(request);
                return Ok(loginResponse);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging in user");
                return StatusCode(500, new { message = "Internal server error" });
            }
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
                // Lấy user ID từ claims (JWT token mới)
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("userId")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    // Fallback: thử lấy từ Clerk (nếu vẫn dùng Clerk token)
                    var clerkUserId = User.FindFirst("sub")?.Value;
                    if (!string.IsNullOrEmpty(clerkUserId))
                    {
                        var clerkUserDto = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
                        if (clerkUserDto == null)
                        {
                            return NotFound("User not found");
                        }
                        return Ok(clerkUserDto);
                    }

                    return Unauthorized("Invalid token");
                }

                // Lấy user từ database bằng userId
                var userDto = await _userService.GetUserByIdAsync(userId);
                
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
        /// Sync user từ Clerk vào database sau khi login
        /// </summary>
        [HttpPost("sync")]
        [Authorize]
        public async Task<IActionResult> SyncUser()
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

                var userDto = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
                if (userDto == null)
                {
                    return NotFound("User not found");
                }

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user");
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
