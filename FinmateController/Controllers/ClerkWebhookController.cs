using Microsoft.AspNetCore.Mvc;
using BLL.Services;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/clerk")]
    public class ClerkWebhookController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ClerkService _clerkService;
        private readonly ILogger<ClerkWebhookController> _logger;

        public ClerkWebhookController(
            UserService userService,
            ClerkService clerkService,
            ILogger<ClerkWebhookController> logger)
        {
            _userService = userService;
            _clerkService = clerkService;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook()
        {
            try
            {
                // Đọc raw body để verify signature
                Request.EnableBuffering();
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                Request.Body.Position = 0;

                // Lấy signature từ header
                var signature = Request.Headers["svix-signature"].FirstOrDefault() 
                    ?? Request.Headers["Clerk-Signature"].FirstOrDefault();

                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("Webhook request missing signature");
                    return Unauthorized("Missing signature");
                }

                // Verify webhook signature
                if (!_clerkService.VerifyWebhookSignature(body, signature))
                {
                    _logger.LogWarning("Invalid webhook signature");
                    return Unauthorized("Invalid signature");
                }

                // Parse webhook event
                var webhookEvent = _clerkService.ParseWebhookEvent(body);
                if (webhookEvent == null || webhookEvent.Data == null)
                {
                    _logger.LogWarning("Failed to parse webhook event");
                    return BadRequest("Invalid webhook payload");
                }

                // Xử lý các loại events
                switch (webhookEvent.Type)
                {
                    case "user.created":
                        await HandleUserCreated(webhookEvent.Data);
                        break;
                    case "user.updated":
                        await HandleUserUpdated(webhookEvent.Data);
                        break;
                    case "user.deleted":
                        await HandleUserDeleted(webhookEvent.Data);
                        break;
                    default:
                        _logger.LogInformation("Unhandled webhook event type: {Type}", webhookEvent.Type);
                        break;
                }

                return Ok(new { received = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task HandleUserCreated(ClerkWebhookData data)
        {
            if (string.IsNullOrEmpty(data.Id))
            {
                _logger.LogWarning("User created event missing user ID");
                return;
            }

            // Kiểm tra xem user đã tồn tại chưa
            var existingUser = await _userService.GetUserByClerkIdAsync(data.Id);

            if (existingUser != null)
            {
                _logger.LogInformation("User with Clerk ID {ClerkId} already exists", data.Id);
                return;
            }

            // Lấy email từ email addresses (lấy email đầu tiên đã verified hoặc email đầu tiên)
            var email = data.EmailAddresses?.FirstOrDefault(e => e.Verified == true)?.EmailAddress
                ?? data.EmailAddresses?.FirstOrDefault()?.EmailAddress
                ?? string.Empty;

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("User created event missing email for Clerk ID {ClerkId}", data.Id);
                return;
            }

            // Tạo user mới thông qua UserService
            await _userService.CreateUserFromWebhookAsync(data);

            _logger.LogInformation("Created user with Clerk ID {ClerkId} and email {Email}", data.Id, email);
        }

        private async Task HandleUserUpdated(ClerkWebhookData data)
        {
            if (string.IsNullOrEmpty(data.Id))
            {
                _logger.LogWarning("User updated event missing user ID");
                return;
            }

            // Update user thông qua UserService
            var updatedUser = await _userService.UpdateUserFromWebhookAsync(data);

            if (updatedUser == null)
            {
                _logger.LogWarning("User with Clerk ID {ClerkId} not found for update, creating new user", data.Id);
                // UserService sẽ tự động tạo user mới nếu không tìm thấy
                return;
            }

            _logger.LogInformation("Updated user with Clerk ID {ClerkId}", data.Id);
        }

        private async Task HandleUserDeleted(ClerkWebhookData data)
        {
            if (string.IsNullOrEmpty(data.Id))
            {
                _logger.LogWarning("User deleted event missing user ID");
                return;
            }

            // Deactivate user thông qua UserService
            var result = await _userService.DeactivateUserAsync(data.Id);

            if (!result)
            {
                _logger.LogWarning("User with Clerk ID {ClerkId} not found for deletion", data.Id);
                return;
            }

            _logger.LogInformation("Deactivated user with Clerk ID {ClerkId}", data.Id);
        }
    }
}
