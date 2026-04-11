using BLL.DTOs.Request;
using BLL.Services;
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
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            UserService userService,
            ChatService chatService,
            ILogger<ChatController> logger)
            : base(userService)
        {
            _chatService = chatService;
            _logger = logger;
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

                var response = await _chatService.SendChatAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("ApiKey") || ex.Message.Contains("chưa được cấu hình"))
            {
                _logger.LogError(ex, "AI ApiKey chưa cấu hình trên server");
                return StatusCode(503, new { message = "AI chưa được cấu hình. Liên hệ quản trị viên." });
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
