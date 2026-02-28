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
    [Authorize(AuthenticationSchemes = "Clerk,Basic")]
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
        /// Hỗ trợ cả token từ Clerk và token basic (scheme "Basic")
        /// Ưu tiên Clerk token trước, fallback sang Basic JWT
        /// </summary>
        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = "Clerk,Basic")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                // Priority 1: Kiểm tra Clerk token ("sub" claim)
                var clerkUserId = User.FindFirst("sub")?.Value;
                if (!string.IsNullOrEmpty(clerkUserId) && !Guid.TryParse(clerkUserId, out _))
                {
                    // "sub" claim tồn tại và KHÔNG phải là Guid => đây là Clerk user ID
                    var clerkUserDto = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
                    if (clerkUserDto != null)
                    {
                        return Ok(clerkUserDto);
                    }
                }

                // Priority 2: Kiểm tra Basic JWT (NameIdentifier/userId là Guid)
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("userId")?.Value;

                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    var userDto = await _userService.GetUserByIdAsync(userId);
                    if (userDto != null)
                    {
                        return Ok(userDto);
                    }
                }

                // Priority 3: Clerk token với NameIdentifier (một số config Clerk dùng NameIdentifier thay vì sub)
                var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(nameIdentifier) && !Guid.TryParse(nameIdentifier, out _))
                {
                    var clerkUserDto = await _userService.GetOrCreateUserFromClerkAsync(nameIdentifier);
                    if (clerkUserDto != null)
                    {
                        return Ok(clerkUserDto);
                    }
                }

                return Unauthorized("Invalid token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Sync user từ Clerk vào database sau khi login
        /// Ưu tiên Clerk token ("sub" claim) trước
        /// </summary>
        [HttpPost("sync")]
        [Authorize(AuthenticationSchemes = "Clerk,Basic")]
        public async Task<IActionResult> SyncUser()
        {
            try
            {
                // Priority 1: Clerk token ("sub" claim)
                var clerkUserId = User.FindFirst("sub")?.Value;
                if (!string.IsNullOrEmpty(clerkUserId) && !Guid.TryParse(clerkUserId, out _))
                {
                    var userDto = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
                    if (userDto != null)
                    {
                        return Ok(userDto);
                    }
                }

                // Priority 2: NameIdentifier là Clerk ID (không phải Guid)
                var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(nameIdentifier) && !Guid.TryParse(nameIdentifier, out _))
                {
                    var userDto = await _userService.GetOrCreateUserFromClerkAsync(nameIdentifier);
                    if (userDto != null)
                    {
                        return Ok(userDto);
                    }
                }

                return Unauthorized("Invalid token - Clerk user ID not found");
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
                    Email = clerkUser.GetPrimaryEmail(),
                    FirstName = clerkUser.FirstName,
                    LastName = clerkUser.LastName,
                    PhoneNumber = clerkUser.PhoneNumber,
                    ImageUrl = clerkUser.ImageUrl,
                    CreatedAt = clerkUser.GetCreatedAtDateTime(),
                    UpdatedAt = clerkUser.GetUpdatedAtDateTime(),
                    LastSignInAt = clerkUser.GetLastSignInAtDateTime()
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
        [Authorize(AuthenticationSchemes = "Clerk,Basic")]
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
