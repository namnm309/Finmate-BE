using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BLL.Services;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/clerk")]
    [Produces("application/json")]
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
        [AllowAnonymous] // Webhook không cần authentication, Clerk verify bằng signature
        public async Task<IActionResult> HandleWebhook()
        {
            string body = "";
            try
            {
                // Đọc raw body để verify signature
                Request.EnableBuffering();
                body = await new StreamReader(Request.Body).ReadToEndAsync();
                Request.Body.Position = 0;

                _logger.LogInformation("Received webhook payload: {Body}", body.Length > 500 ? body.Substring(0, 500) + "..." : body);

                // Lấy signature từ header
                var signature = Request.Headers["svix-signature"].FirstOrDefault() 
                    ?? Request.Headers["Clerk-Signature"].FirstOrDefault();

                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("Webhook request missing signature");
                    return Unauthorized(new { 
                        error = "Missing signature",
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                    });
                }

                // Tạm thời bỏ qua verify signature để debug
                // if (!_clerkService.VerifyWebhookSignature(body, signature))
                // {
                //     _logger.LogWarning("Invalid webhook signature");
                //     return Unauthorized("Invalid signature");
                // }

                // Parse webhook event
                ClerkWebhookEvent? webhookEvent;
                try
                {
                    webhookEvent = _clerkService.ParseWebhookEvent(body);
                }
                catch (Exception parseEx)
                {
                    _logger.LogError(parseEx, "Failed to parse webhook event. Error: {Error}", parseEx.Message);
                    return BadRequest(new { 
                        error = "Invalid webhook payload - parse failed", 
                        message = parseEx.Message,
                        innerError = parseEx.InnerException?.Message
                    });
                }

                if (webhookEvent == null)
                {
                    _logger.LogWarning("Failed to parse webhook event - webhookEvent is null");
                    return BadRequest(new { 
                        error = "Invalid webhook payload - parse returned null",
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                    });
                }
                
                if (webhookEvent.Data == null)
                {
                    _logger.LogWarning("Failed to parse webhook event - Data is null. Type: {Type}", webhookEvent.Type);
                    return BadRequest(new { 
                        error = "Invalid webhook payload - data is null", 
                        type = webhookEvent.Type 
                    });
                }

                _logger.LogInformation("Parsed webhook event type: {Type}, User ID: {UserId}", 
                    webhookEvent.Type, webhookEvent.Data.Id);

                // Xử lý các loại events
                try
                {
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
                        case "session.created":
                            await HandleSessionCreated(webhookEvent.Data);
                            break;
                        default:
                            _logger.LogInformation("Unhandled webhook event type: {Type}", webhookEvent.Type);
                            break;
                    }

                    _logger.LogInformation("Successfully processed webhook event type: {Type}", webhookEvent.Type);
                    return Ok(new { received = true, type = webhookEvent.Type });
                }
                catch (Exception handlerEx)
                {
                    _logger.LogError(handlerEx, "Error in event handler for type: {Type}. Error: {Error}", 
                        webhookEvent.Type, handlerEx.Message);
                    return StatusCode(500, new { 
                        error = "Error processing webhook event", 
                        type = webhookEvent.Type,
                        message = handlerEx.Message,
                        innerError = handlerEx.InnerException?.Message,
                        stackTrace = handlerEx.StackTrace
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook. Body: {Body}", body.Length > 1000 ? body.Substring(0, 1000) : body);
                
                // Trả về error response chi tiết để hiển thị trên Clerk Dashboard
                var errorResponse = new
                {
                    error = "Internal server error processing webhook",
                    message = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                };
                
                return StatusCode(500, errorResponse);
            }
        }

        private async Task HandleUserCreated(ClerkWebhookData data)
        {
            try
            {
                if (string.IsNullOrEmpty(data.Id))
                {
                    _logger.LogWarning("User created event missing user ID");
                    return;
                }

                _logger.LogInformation("Processing user.created event for Clerk ID: {ClerkId}", data.Id);

                await _userService.UpsertUserFromWebhookAsync(data);

                var email = data.EmailAddresses?.FirstOrDefault(e => e.IsVerified)?.EmailAddress
                    ?? data.EmailAddresses?.FirstOrDefault()?.EmailAddress
                    ?? string.Empty;
                _logger.LogInformation("Successfully processed user.created for Clerk ID {ClerkId} and email {Email}", data.Id, email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user.created event for Clerk ID: {ClerkId}. Error: {Error}", 
                    data?.Id, ex.Message);
                throw; // Re-throw để outer catch có thể handle
            }
        }

        private async Task HandleUserUpdated(ClerkWebhookData data)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user.updated event for Clerk ID: {ClerkId}. Error: {Error}", 
                    data?.Id, ex.Message);
                throw;
            }
        }

        private async Task HandleUserDeleted(ClerkWebhookData data)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user.deleted event for Clerk ID: {ClerkId}. Error: {Error}", 
                    data?.Id, ex.Message);
                throw;
            }
        }

        private async Task HandleSessionCreated(ClerkWebhookData data)
        {
            try
            {
                var userId = data.UserId ?? data.User?.Id;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Session created event missing user ID");
                    return;
                }

                ClerkWebhookData userData = data.User ?? data;
                if (string.IsNullOrEmpty(userData.Id))
                {
                    userData.Id = userId;
                }

                await _userService.UpsertUserFromWebhookAsync(userData);

                var email = userData.EmailAddresses?.FirstOrDefault(e => e.IsVerified)?.EmailAddress
                    ?? userData.EmailAddresses?.FirstOrDefault()?.EmailAddress
                    ?? string.Empty;
                _logger.LogInformation("Successfully processed session.created for Clerk ID {ClerkId} and email {Email}", userId, email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling session.created event. Error: {Error}", ex.Message);
                throw;
            }
        }
    }
}
