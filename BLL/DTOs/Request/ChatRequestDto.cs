using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    /// <summary>
    /// Request gửi tin nhắn đến AI chat bot
    /// </summary>
    public class ChatRequestDto
    {
        /// <summary>
        /// Nội dung tin nhắn (dùng khi chat đơn giản 1 lượt)
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Lịch sử hội thoại (dùng khi cần context nhiều lượt)
        /// </summary>
        public List<ChatMessageDto>? Messages { get; set; }

        /// <summary>
        /// System prompt tùy chỉnh (hành vi của AI)
        /// </summary>
        public string? SystemPrompt { get; set; }

        /// <summary>
        /// Ảnh hóa đơn dạng base64 (dùng cho quét hóa đơn - vision)
        /// </summary>
        public string? ImageBase64 { get; set; }

        /// <summary>
        /// Định dạng ảnh: "png" hoặc "jpeg" (mặc định "jpeg")
        /// </summary>
        public string? ImageFormat { get; set; }

        /// <summary>
        /// Model tùy chọn để override khi test hoặc ép dùng model cụ thể.
        /// </summary>
        public string? Model { get; set; }
    }
}
