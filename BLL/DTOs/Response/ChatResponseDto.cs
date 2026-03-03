namespace BLL.DTOs.Response
{
    /// <summary>
    /// Response từ AI chat bot
    /// </summary>
    public class ChatResponseDto
    {
        public string Content { get; set; } = string.Empty;

        public ChatUsageDto? Usage { get; set; }
    }

    public class ChatUsageDto
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
