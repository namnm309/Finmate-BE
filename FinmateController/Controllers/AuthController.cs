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
    [Authorize(AuthenticationSchemes = "Clerk")]
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
        /// Lấy thông tin user hiện tại từ Clerk JWT token.
        /// Tự động tạo user trong DB nếu chưa tồn tại.
        /// </summary>
        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = "Clerk")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                // Clerk token dùng claim "sub" làm user ID
                var clerkUserId = User.FindFirst("sub")?.Value;
                if (!string.IsNullOrEmpty(clerkUserId))
                {
                    var clerkUserDto = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
                    if (clerkUserDto != null)
                    {
                        return Ok(clerkUserDto);
                    }
                }

                // Fallback: một số cấu hình Clerk dùng NameIdentifier thay vì sub
                var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(nameIdentifier))
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
        /// Sync user từ Clerk vào database sau khi login.
        /// </summary>
        [HttpPost("sync")]
        [Authorize(AuthenticationSchemes = "Clerk")]
        public async Task<IActionResult> SyncUser()
        {
            try
            {
                var clerkUserId = User.FindFirst("sub")?.Value;
                if (!string.IsNullOrEmpty(clerkUserId))
                {
                    var userDto = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
                    if (userDto != null)
                    {
                        return Ok(userDto);
                    }
                }

                var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(nameIdentifier))
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
        /// Verify Clerk token và lấy thông tin user từ Clerk API.
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
        /// Test endpoint để kiểm tra Clerk authentication.
        /// </summary>
        [HttpGet("test")]
        [Authorize(AuthenticationSchemes = "Clerk")]
        public IActionResult TestAuth()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Ok(new
            {
                message = "Clerk authentication successful",
                claims = claims,
                clerkUserId = User.FindFirst("sub")?.Value
            });
        }
    }
}
