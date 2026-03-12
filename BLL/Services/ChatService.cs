using System.Net.Http.Json;
using System.Text;
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
        /// Liệt kê các model vision có sẵn trên MegaLLM (free tier)
        /// </summary>
        public async Task<List<string>> GetVisionModelsAsync(CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["MegaLLM:ApiKey"];
            var baseUrl = _configuration["MegaLLM:BaseUrl"]?.TrimEnd('/') ?? "https://ai.megallm.io/v1";
            if (string.IsNullOrWhiteSpace(apiKey)) return new List<string>();

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/models");
                req.Headers.Add("Authorization", $"Bearer {apiKey}");
                var response = await _httpClient.SendAsync(req, cancellationToken);
                if (!response.IsSuccessStatusCode) return new List<string>();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var visionModels = new List<string>();
                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    foreach (var model in data.EnumerateArray())
                    {
                        if (model.TryGetProperty("capabilities", out var caps) &&
                            caps.TryGetProperty("supports_vision", out var sv) && sv.GetBoolean())
                        {
                            if (model.TryGetProperty("id", out var id))
                                visionModels.Add(id.GetString() ?? "");
                        }
                    }
                }
                return visionModels;
            }
            catch { return new List<string>(); }
        }

        /// <summary>
        /// Kiểm tra cấu hình Mega LLM (OpenAI-compatible)
        /// </summary>
        public async Task<(bool ApiKeyConfigured, string Provider, string BaseUrl, string ModelId, string VisionModelId, string? TestError)> GetDiagnosticAsync(CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["MegaLLM:ApiKey"];
            var baseUrl = _configuration["MegaLLM:BaseUrl"]?.TrimEnd('/') ?? "https://ai.megallm.io/v1";
            var modelId = _configuration["MegaLLM:ModelId"] ?? "openai-gpt-oss-20b";
            var visionModelId = _configuration["MegaLLM:VisionModelId"] ?? "alibaba-qwen3.5-397b";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return (false, "MegaLLM", baseUrl, modelId, visionModelId, "MegaLLM:ApiKey chưa được cấu hình.");
            }

            try
            {
                var url = $"{baseUrl}/chat/completions";
                var body = new MegaLLMRequest
                {
                    Model = modelId,
                    Messages = new List<object> { new MegaLLMTextMessage { Role = "user", Content = "Hello" } },
                    MaxTokens = 5,
                    Temperature = 0.1
                };
                var jsonBody = JsonSerializer.Serialize(body, JsonOptions);
                using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(jsonBody, Encoding.UTF8, "application/json") };
                req.Headers.Add("Authorization", $"Bearer {apiKey}");
                var response = await _httpClient.SendAsync(req, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync(cancellationToken);
                    return (true, "MegaLLM", baseUrl, modelId, visionModelId, $"Mega LLM trả {(int)response.StatusCode}: {err}");
                }
                return (true, "MegaLLM", baseUrl, modelId, visionModelId, null);
            }
            catch (Exception ex)
            {
                return (true, "MegaLLM", baseUrl, modelId, visionModelId, ex.Message);
            }
        }

        /// <summary>
        /// Gửi tin nhắn đến Mega LLM (OpenAI-compatible) và nhận phản hồi.
        /// Hỗ trợ vision: khi có ImageBase64, message cuối cùng sẽ là multimodal (text + image_url).
        /// </summary>
        public async Task<ChatResponseDto> SendChatAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["MegaLLM:ApiKey"];
            var baseUrl = _configuration["MegaLLM:BaseUrl"]?.TrimEnd('/') ?? "https://ai.megallm.io/v1";
            var maxTokens = _configuration.GetValue<int>("MegaLLM:MaxTokens", 4096);
            var temperature = _configuration.GetValue<double>("MegaLLM:Temperature", 0.7);

            var hasImage = !string.IsNullOrWhiteSpace(request.ImageBase64);
            var textModelId = _configuration["MegaLLM:ModelId"] ?? "openai-gpt-oss-20b";
            var visionModelId = _configuration["MegaLLM:VisionModelId"] ?? "alibaba-qwen3.5-397b";
            var requestModel = request.Model?.Trim();
            var modelId = !string.IsNullOrWhiteSpace(requestModel)
                ? requestModel
                : (hasImage ? visionModelId : textModelId);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("MegaLLM:ApiKey chưa được cấu hình. Thêm MegaLLM__ApiKey vào Azure Application Settings.");
            }

            var systemPrompt = request.SystemPrompt?.Trim();
            if (string.IsNullOrEmpty(systemPrompt))
            {
                systemPrompt = "Bạn là trợ lý tài chính cá nhân thông minh của ứng dụng Finmate. Trả lời ngắn gọn bằng tiếng Việt.";
            }

            // Build danh sách messages đầy đủ cho MegaLLM
            var messages = new List<object>();

            // 1. System prompt
            messages.Add(new MegaLLMTextMessage { Role = "system", Content = systemPrompt });

            // 2. Lịch sử hội thoại (bỏ qua message system từ client nếu có)
            if (request.Messages != null && request.Messages.Count > 0)
            {
                var historyMessages = request.Messages
                    .Where(m => !string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                for (int i = 0; i < historyMessages.Count; i++)
                {
                    var msg = historyMessages[i];
                    bool isLastUserMsg = hasImage
                        && i == historyMessages.Count - 1
                        && string.Equals(msg.Role, "user", StringComparison.OrdinalIgnoreCase);

                    if (isLastUserMsg)
                    {
                        // Vision message: OpenAI format chính xác [text, image_url]
                        var visionContent = new List<object>
                        {
                            new Dictionary<string, object> { ["type"] = "text", ["text"] = msg.Content ?? "" },
                            new Dictionary<string, object>
                            {
                                ["type"] = "image_url",
                                ["image_url"] = new Dictionary<string, object>
                                {
                                    ["url"] = BuildImageDataUrl(request.ImageBase64!, request.ImageFormat),
                                    ["detail"] = "high"
                                }
                            }
                        };
                        messages.Add(new MegaLLMVisionMessage { Role = "user", Content = visionContent });
                    }
                    else
                    {
                        messages.Add(new MegaLLMTextMessage { Role = msg.Role ?? "user", Content = msg.Content ?? "" });
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.Message))
            {
                // Fallback: dùng Message đơn giản
                if (hasImage)
                {
                    var visionContent = new List<object>
                    {
                        new Dictionary<string, object> { ["type"] = "text", ["text"] = request.Message },
                        new Dictionary<string, object>
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new Dictionary<string, object>
                            {
                                ["url"] = BuildImageDataUrl(request.ImageBase64!, request.ImageFormat),
                                ["detail"] = "high"
                            }
                        }
                    };
                    messages.Add(new MegaLLMVisionMessage { Role = "user", Content = visionContent });
                }
                else
                {
                    messages.Add(new MegaLLMTextMessage { Role = "user", Content = request.Message });
                }
            }
            else
            {
                throw new ArgumentException("Message hoặc Messages không được để trống");
            }

            var megaRequest = new MegaLLMRequest
            {
                Model = modelId,
                Messages = messages,
                MaxTokens = maxTokens,
                Temperature = temperature
            };

            var url = $"{baseUrl}/chat/completions";
            var jsonBody = JsonSerializer.Serialize(megaRequest, JsonOptions);
            _logger.LogInformation("Mega LLM request Model: {Model}, TextModel: {TextModel}, VisionModel: {VisionModel}, HasImage: {HasImage}, ImageBase64Len: {Len}, MessageCount: {Count}",
                modelId, textModelId, visionModelId, hasImage, hasImage ? (request.ImageBase64?.Length ?? 0) : 0, messages.Count);

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            requestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
            requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Mega LLM API error {StatusCode} Model={Model}: {Body}", response.StatusCode, modelId, errorBody);

                // Gợi ý rõ ràng khi model không tồn tại hoặc không hỗ trợ vision
                var status = (int)response.StatusCode;
                if (hasImage && (status == 400 || status == 404 || status == 422))
                {
                    throw new HttpRequestException(
                        $"Model '{modelId}' không hỗ trợ vision hoặc không tồn tại trong gói hiện tại. " +
                        $"Hãy kiểm tra dashboard MegaLLM (megallm.io/dashboard) để tìm model ID có vision, " +
                        $"rồi thêm MegaLLM__VisionModelId vào Azure Application Settings. " +
                        $"Chi tiết: {errorBody}");
                }

                throw new HttpRequestException($"Mega LLM trả lỗi {status}: {errorBody}");
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<MegaLLMResponse>(JsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Không thể đọc phản hồi từ Mega LLM");

            var text = apiResponse.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
            var usage = apiResponse.Usage != null
                ? new ChatUsageDto
                {
                    PromptTokens = apiResponse.Usage.PromptTokens,
                    CompletionTokens = apiResponse.Usage.CompletionTokens,
                    TotalTokens = apiResponse.Usage.TotalTokens
                }
                : null;

            return new ChatResponseDto { Content = text, Usage = usage };
        }

        private static string BuildImageDataUrl(string base64, string? format)
        {
            var mime = string.Equals(format, "png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
            return $"data:{mime};base64,{base64}";
        }

        #region Mega LLM (OpenAI-compatible) Models

        private class MegaLLMRequest
        {
            public string Model { get; set; } = "";
            public List<object> Messages { get; set; } = new();
            public int MaxTokens { get; set; }
            public double Temperature { get; set; }
        }

        // Text-only message: { "role": "user", "content": "..." }
        private class MegaLLMTextMessage
        {
            public string Role { get; set; } = "";
            public string Content { get; set; } = "";
        }

        // Vision message: { "role": "user", "content": [...] }
        private class MegaLLMVisionMessage
        {
            public string Role { get; set; } = "";
            public List<object> Content { get; set; } = new();
        }

        // Text part trong vision content
        private class MegaLLMTextPart
        {
            public string Type { get; set; } = "text";
            public string Text { get; set; } = "";
        }

        // Image part trong vision content
        private class MegaLLMImagePart
        {
            public string Type { get; set; } = "image_url";
            [JsonPropertyName("image_url")]
            public MegaLLMImageUrl ImageUrl { get; set; } = new();
        }

        private class MegaLLMImageUrl
        {
            public string Url { get; set; } = "";
            public string Detail { get; set; } = "auto";
        }

        private class MegaLLMResponse
        {
            public List<MegaLLMChoice>? Choices { get; set; }
            public MegaLLMUsage? Usage { get; set; }
        }

        private class MegaLLMChoice
        {
            public MegaLLMTextMessage? Message { get; set; }
        }

        private class MegaLLMUsage
        {
            public int PromptTokens { get; set; }
            public int CompletionTokens { get; set; }
            public int TotalTokens { get; set; }
        }

        #endregion
    }
}
