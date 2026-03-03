using BLL.DTOs.Request;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinmateController.Controllers
{
    /// <summary>
    /// AI Chat Bot - tích hợp MegaLLM (GPT-4o-mini)
    /// </summary>
    [ApiController]
    [Route("api/chat")]
    [Authorize(AuthenticationSchemes = "Clerk,Basic")]
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
                _logger.LogError(ex, "MegaLLM ApiKey chưa cấu hình trên server");
                return StatusCode(503, new { message = "AI chưa được cấu hình. Liên hệ quản trị viên." });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "MegaLLM API error: {Message}", ex.Message);
                return StatusCode(502, new { message = "Không thể kết nối AI. Vui lòng thử lại sau." });
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning(ex, "MegaLLM timeout");
                return StatusCode(504, new { message = "AI phản hồi quá chậm. Vui lòng thử lại." });
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("MegaLLM request cancelled/timeout");
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
