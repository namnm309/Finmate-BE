using BLL.DTOs.Request;
using BLL.DTOs.Response;
using BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinmateController.Controllers
{
    /// <summary>
    /// Basic username/password auth (không dùng Clerk)
    /// </summary>
    [ApiController]
    [Route("api/basic-auth")]
    public class BasicAuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<BasicAuthController> _logger;

        public BasicAuthController(AuthService authService, ILogger<BasicAuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Đăng ký tài khoản mới (basic, không dùng Clerk)
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
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering basic user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Đăng nhập với email/password và nhận JWT token (basic)
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
                _logger.LogError(ex, "Error logging in basic user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
