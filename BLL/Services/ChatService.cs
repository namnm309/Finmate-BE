using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BLL.DTOs.Request;
using BLL.DTOs.Response;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class ChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public ChatService(HttpClient httpClient, IConfiguration configuration, ILogger<ChatService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Gửi tin nhắn đến AI và nhận phản hồi (MegaLLM - GPT-4o-mini)
        /// Hỗ trợ vision khi có ImageBase64 (quét hóa đơn)
        /// </summary>
        public async Task<ChatResponseDto> SendChatAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["MegaLLM:ApiKey"];
            var baseUrl = _configuration["MegaLLM:BaseUrl"] ?? "https://ai.megallm.io/v1";
            var hasImage = !string.IsNullOrWhiteSpace(request.ImageBase64);
            var modelId = hasImage
                ? (_configuration["MegaLLM:VisionModelId"] ?? "gpt-4o-mini")
                : (_configuration["MegaLLM:ModelId"] ?? "gpt-4o-mini");
            var maxTokens = _configuration.GetValue<int>("MegaLLM:MaxTokens", 4096);
            var temperature = _configuration.GetValue<double>("MegaLLM:Temperature", 0.7);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("MegaLLM:ApiKey chưa được cấu hình trong appsettings.json");
            }

            var messages = BuildMessages(request, hasImage, request.ImageBase64?.Trim());
            if (messages.Count == 0)
            {
                throw new ArgumentException("Message hoặc Messages không được để trống");
            }

            var apiRequest = new MegaLLMChatRequest
            {
                Model = modelId,
                Messages = messages,
                MaxTokens = maxTokens,
                Temperature = temperature
            };

            var url = baseUrl.TrimEnd('/') + "/chat/completions";
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            requestMessage.Headers.Add("Authorization", "Bearer " + apiKey);
            requestMessage.Content = JsonContent.Create(apiRequest, options: JsonOptions);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("MegaLLM API error {StatusCode}: {Body}", response.StatusCode, errorBody);
                throw new HttpRequestException($"MegaLLM API lỗi: {response.StatusCode}. {errorBody}");
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<MegaLLMChatResponse>(JsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Không thể đọc phản hồi từ MegaLLM");

            return MapToResponse(apiResponse);
        }

        private static List<MegaLLMMessage> BuildMessages(ChatRequestDto request, bool hasImage, string? imageBase64)
        {
            var messages = new List<MegaLLMMessage>();

            var systemPrompt = request.SystemPrompt?.Trim();
            if (string.IsNullOrEmpty(systemPrompt))
            {
                systemPrompt = "Bạn là trợ lý tài chính cá nhân thông minh của ứng dụng Finmate. Bạn giúp người dùng quản lý chi tiêu, đưa ra lời khuyên tiết kiệm và tối ưu tài chính. " +
                    "Khi người dùng gửi ảnh hóa đơn, hãy quét và trích xuất: tổng số tiền chi tiêu, ngày, danh sách món hàng. Trả lời ngắn gọn bằng tiếng Việt.";
            }
            messages.Add(new MegaLLMMessage { Role = "system", Content = systemPrompt });

            string? userText = null;

            if (request.Messages != null && request.Messages.Count > 0)
            {
                var lastIdx = request.Messages.Count - 1;
                for (var i = 0; i < request.Messages.Count; i++)
                {
                    var m = request.Messages[i];
                    if (string.IsNullOrWhiteSpace(m.Content)) continue;

                    var role = m.Role?.ToLowerInvariant() ?? "user";
                    if (i == lastIdx && role == "user" && hasImage)
                    {
                        userText = m.Content.Trim();
                        break;
                    }
                    messages.Add(new MegaLLMMessage { Role = role, Content = m.Content.Trim() });
                }
                if (userText == null)
                    userText = request.Messages.LastOrDefault(m => m.Role?.Equals("user", StringComparison.OrdinalIgnoreCase) == true)?.Content?.Trim();
            }

            if (userText == null || (hasImage && string.IsNullOrWhiteSpace(userText)))
                userText = request.Message?.Trim();
            if (string.IsNullOrWhiteSpace(userText) && !hasImage) return messages;

            if (hasImage && !string.IsNullOrWhiteSpace(imageBase64))
            {
                var prompt = !string.IsNullOrWhiteSpace(userText) ? userText : "Hãy quét hóa đơn này và trích xuất tổng số tiền chi tiêu, ngày, danh sách món hàng.";
                var contentParts = new List<object>
                {
                    new { type = "text", text = prompt },
                    new { type = "image_url", image_url = new { url = "data:image/jpeg;base64," + imageBase64 } }
                };
                messages.Add(new MegaLLMMessage { Role = "user", Content = contentParts });
            }
            else if (!string.IsNullOrWhiteSpace(userText))
            {
                messages.Add(new MegaLLMMessage { Role = "user", Content = userText });
            }

            return messages;
        }

        private static ChatResponseDto MapToResponse(MegaLLMChatResponse apiResponse)
        {
            var raw = apiResponse.Choices?.FirstOrDefault()?.Message?.Content;
            var content = raw is string s ? s : (raw?.ToString() ?? string.Empty);
            var usage = apiResponse.Usage != null
                ? new ChatUsageDto
                {
                    PromptTokens = apiResponse.Usage.PromptTokens,
                    CompletionTokens = apiResponse.Usage.CompletionTokens,
                    TotalTokens = apiResponse.Usage.TotalTokens
                }
                : null;

            return new ChatResponseDto { Content = content, Usage = usage };
        }

        #region MegaLLM API Models (snake_case)

        private class MegaLLMChatRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = "";

            [JsonPropertyName("messages")]
            public List<MegaLLMMessage> Messages { get; set; } = new();

            [JsonPropertyName("max_tokens")]
            public int MaxTokens { get; set; }

            [JsonPropertyName("temperature")]
            public double Temperature { get; set; }
        }

        private class MegaLLMMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = "user";

            [JsonPropertyName("content")]
            public object Content { get; set; } = "";
        }

        private class MegaLLMChatResponse
        {
            [JsonPropertyName("choices")]
            public List<MegaLLMChoice>? Choices { get; set; }

            [JsonPropertyName("usage")]
            public MegaLLMUsage? Usage { get; set; }
        }

        private class MegaLLMChoice
        {
            [JsonPropertyName("message")]
            public MegaLLMMessage? Message { get; set; }
        }

        private class MegaLLMUsage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }

        #endregion
    }
}
