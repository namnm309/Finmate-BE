using BLL.DTOs.Request;
using BLL.Services;
using BLL.Services.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinmateController.Controllers
{
    /// <summary>
    /// AI Chat Bot — OpenRouter hoặc MegaLLM (AISupport:Provider), OpenAI-compatible.
    /// </summary>
    [ApiController]
    [Route("api/chat")]
    [Authorize(AuthenticationSchemes = "Clerk")]
    public class ChatController : FinmateControllerBase
    {
        private readonly ChatService _chatService;
        private readonly AiUsageService _aiUsageService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            UserService userService,
            ChatService chatService,
            AiUsageService aiUsageService,
            ILogger<ChatController> logger)
            : base(userService)
        {
            _chatService = chatService;
            _aiUsageService = aiUsageService;
            _logger = logger;
        }

        private static AiFeatureKind ParseAiFeature(ChatRequestDto request)
        {
            var raw = request.AiFeature?.Trim().ToLowerInvariant();
            if (raw == "plan" || raw == "planner" || raw == "financial_plan" || raw == "budget_plan")
                return AiFeatureKind.Plan;
            return AiFeatureKind.Chat;
        }

        /// <summary>
        /// Kiểm tra cấu hình AI (OpenRouter / MegaLLM)
        /// </summary>
        [HttpGet("diagnostic")]
        [AllowAnonymous]
        public async Task<IActionResult> Diagnostic(CancellationToken cancellationToken = default)
        {
            var (apiKeyConfigured, provider, baseUrl, modelId, testError) = await _chatService.GetDiagnosticAsync(cancellationToken);
            var visionModels = await _chatService.GetVisionModelsAsync(cancellationToken);
            return Ok(new
            {
                apiKeyConfigured,
                provider,
                baseUrl,
                modelId,
                status = testError == null ? "OK" : "Error",
                error = testError,
                visionModels,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Gửi tin nhắn đến AI chat bot và nhận phản hồi (không cần đăng nhập)
        /// </summary>
        /// <param name="request">Nội dung tin nhắn hoặc lịch sử hội thoại</param>
        /// <returns>Phản hồi từ AI</returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Request không được để trống" });
                }

                Guid? dbUserId = null;
                var feature = ParseAiFeature(request);
                if (User?.Identity?.IsAuthenticated == true)
                {
                    dbUserId = await GetCurrentUserIdAsync();
                    if (dbUserId.HasValue)
                        await _aiUsageService.EnsureCanCallAsync(dbUserId.Value, feature, cancellationToken);
                }

                var response = await _chatService.SendChatAsync(request, cancellationToken);

                if (dbUserId.HasValue)
                {
                    try
                    {
                        await _aiUsageService.IncrementAsync(dbUserId.Value, feature, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "AI usage increment failed after successful chat");
                    }
                }

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("ApiKey") || ex.Message.Contains("ModelId") || ex.Message.Contains("chưa được cấu hình"))
            {
                _logger.LogError(ex, "AI thiếu cấu hình trên server (ApiKey / ModelId)");
                return StatusCode(503, new { message = "AI chưa được cấu hình. Liên hệ quản trị viên." });
            }
            catch (AiQuotaExceededException ex)
            {
                _logger.LogWarning(ex, "AI quota exceeded for user");
                return StatusCode(429, new { message = ex.Message, code = "ai_quota_exceeded" });
            }
            catch (AiRateLimitedException ex)
            {
                _logger.LogWarning(ex, "AI rate limited (429 upstream)");
                return StatusCode(429, new { message = ex.Message, code = "ai_rate_limited" });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "AI API error: {Message}", ex.Message);
                return StatusCode(502, new { message = "Không thể kết nối AI. Vui lòng thử lại sau.", detail = ex.Message });
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning(ex, "AI chat timeout");
                return StatusCode(504, new { message = "AI phản hồi quá chậm. Vui lòng thử lại." });
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("AI chat request cancelled/timeout");
                return StatusCode(504, new { message = "Yêu cầu quá thời gian. Vui lòng thử lại." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in chat");
                return StatusCode(500, new { message = "Lỗi nội bộ. Vui lòng thử lại sau." });
            }
        }
    }
}
