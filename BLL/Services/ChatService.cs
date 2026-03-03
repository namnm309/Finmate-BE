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
        /// Kiểm tra cấu hình Mega LLM (OpenAI-compatible)
        /// </summary>
        public async Task<(bool ApiKeyConfigured, string Provider, string BaseUrl, string ModelId, string? TestError)> GetDiagnosticAsync(CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["MegaLLM:ApiKey"];
            var baseUrl = _configuration["MegaLLM:BaseUrl"]?.TrimEnd('/') ?? "https://ai.megallm.io/v1";
            var modelId = _configuration["MegaLLM:ModelId"] ?? "gpt-5-mini";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return (false, "MegaLLM", baseUrl, modelId, "MegaLLM:ApiKey chưa được cấu hình.");
            }

            try
            {
                var url = $"{baseUrl}/chat/completions";
                var body = new MegaLLMRequest
                {
                    Model = modelId,
                    Messages = new List<MegaLLMMessage> { new() { Role = "user", Content = "Hello" } },
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
                    return (true, "MegaLLM", baseUrl, modelId, $"Mega LLM trả {(int)response.StatusCode}: {err}");
                }
                return (true, "MegaLLM", baseUrl, modelId, null);
            }
            catch (Exception ex)
            {
                return (true, "MegaLLM", baseUrl, modelId, ex.Message);
            }
        }

        /// <summary>
        /// Gửi tin nhắn đến Mega LLM (OpenAI-compatible) và nhận phản hồi
        /// </summary>
        public async Task<ChatResponseDto> SendChatAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["MegaLLM:ApiKey"];
            var baseUrl = _configuration["MegaLLM:BaseUrl"]?.TrimEnd('/') ?? "https://ai.megallm.io/v1";
            var modelId = _configuration["MegaLLM:ModelId"] ?? "gpt-5-mini";
            var maxTokens = _configuration.GetValue<int>("MegaLLM:MaxTokens", 4096);
            var temperature = _configuration.GetValue<double>("MegaLLM:Temperature", 0.7);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("MegaLLM:ApiKey chưa được cấu hình. Thêm MegaLLM__ApiKey vào Azure Application Settings.");
            }

            var systemPrompt = request.SystemPrompt?.Trim();
            if (string.IsNullOrEmpty(systemPrompt))
            {
                systemPrompt = "Bạn là trợ lý tài chính cá nhân thông minh của ứng dụng Finmate. Trả lời ngắn gọn bằng tiếng Việt.";
            }

            var userText = request.Message?.Trim();
            if (request.Messages != null && request.Messages.Count > 0)
            {
                userText = request.Messages.LastOrDefault(m => m.Role?.Equals("user", StringComparison.OrdinalIgnoreCase) == true)?.Content?.Trim() ?? userText;
            }

            if (string.IsNullOrWhiteSpace(userText))
            {
                throw new ArgumentException("Message hoặc Messages không được để trống");
            }

            var messages = new List<MegaLLMMessage>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = userText }
            };

            var megaRequest = new MegaLLMRequest
            {
                Model = modelId,
                Messages = messages,
                MaxTokens = maxTokens,
                Temperature = temperature
            };

            var url = $"{baseUrl}/chat/completions";
            var jsonBody = JsonSerializer.Serialize(megaRequest, JsonOptions);
            _logger.LogInformation("Mega LLM request Model: {Model}", modelId);

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            requestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
            requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Mega LLM API error {StatusCode}: {Body}", response.StatusCode, errorBody);
                throw new HttpRequestException($"Mega LLM trả lỗi {(int)response.StatusCode}: {errorBody}");
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

        #region Mega LLM (OpenAI-compatible) Models

        private class MegaLLMRequest
        {
            public string Model { get; set; } = "";
            public List<MegaLLMMessage> Messages { get; set; } = new();
            public int MaxTokens { get; set; }
            public double Temperature { get; set; }
        }

        private class MegaLLMMessage
        {
            public string Role { get; set; } = "";
            public string Content { get; set; } = "";
        }

        private class MegaLLMResponse
        {
            public List<MegaLLMChoice>? Choices { get; set; }
            public MegaLLMUsage? Usage { get; set; }
        }

        private class MegaLLMChoice
        {
            public MegaLLMMessage? Message { get; set; }
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
