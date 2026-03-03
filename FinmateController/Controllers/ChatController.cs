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
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "MegaLLM API error");
                return StatusCode(502, new { message = "Không thể kết nối AI. Vui lòng thử lại sau." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in chat");
                return StatusCode(500, new { message = "Lỗi nội bộ. Vui lòng thử lại sau." });
            }
        }
    }
}
