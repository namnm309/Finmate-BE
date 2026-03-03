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

        private static readonly JsonSerializerOptions GeminiJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public ChatService(HttpClient httpClient, IConfiguration configuration, ILogger<ChatService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Kiểm tra cấu hình Gemini
        /// </summary>
        public async Task<(bool ApiKeyConfigured, string Provider, string BaseUrl, string ModelId, string? TestError)> GetDiagnosticAsync(CancellationToken cancellationToken = default)
        {
            var geminiKey = _configuration["Gemini:ApiKey"];
            var modelId = _configuration["Gemini:ModelId"] ?? "gemini-1.5-flash";

            if (string.IsNullOrWhiteSpace(geminiKey))
            {
                return (false, "Gemini", "generativelanguage.googleapis.com", modelId, "Gemini:ApiKey chưa được cấu hình.");
            }

            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent";
                var body = new GeminiRequest
                {
                    Contents = new List<GeminiContent> { new() { Parts = new List<GeminiPart> { new() { Text = "Hello" } } } },
                    GenerationConfig = new GeminiGenerationConfig { MaxOutputTokens = 5, Temperature = 0.1 }
                };
                var jsonBody = JsonSerializer.Serialize(body, GeminiJsonOptions);
                using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(jsonBody, Encoding.UTF8, "application/json") };
                req.Headers.Add("x-goog-api-key", geminiKey);
                var response = await _httpClient.SendAsync(req, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync(cancellationToken);
                    return (true, "Gemini", "generativelanguage.googleapis.com", modelId, $"Gemini trả {(int)response.StatusCode}: {err}");
                }
                return (true, "Gemini", "generativelanguage.googleapis.com", modelId, null);
            }
            catch (Exception ex)
            {
                return (true, "Gemini", "generativelanguage.googleapis.com", modelId, ex.Message);
            }
        }

        /// <summary>
        /// Gửi tin nhắn đến Gemini AI và nhận phản hồi
        /// </summary>
        public async Task<ChatResponseDto> SendChatAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            var modelId = _configuration["Gemini:ModelId"] ?? "gemini-1.5-flash";
            var maxTokens = _configuration.GetValue<int>("Gemini:MaxTokens", 4096);
            var temperature = _configuration.GetValue<double>("Gemini:Temperature", 0.7);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Gemini:ApiKey chưa được cấu hình trong appsettings.json");
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

            var geminiRequest = new GeminiRequest
            {
                Contents = new List<GeminiContent>
                {
                    new() { Parts = new List<GeminiPart> { new() { Text = userText } } }
                },
                SystemInstruction = new GeminiSystemInstruction { Parts = new List<GeminiPart> { new() { Text = systemPrompt } } },
                GenerationConfig = new GeminiGenerationConfig
                {
                    MaxOutputTokens = maxTokens,
                    Temperature = temperature
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent";
            var jsonBody = JsonSerializer.Serialize(geminiRequest, GeminiJsonOptions);
            _logger.LogInformation("Gemini request Model: {Model}", modelId);

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            requestMessage.Headers.Add("x-goog-api-key", apiKey);
            requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error {StatusCode}: {Body}", response.StatusCode, errorBody);
                throw new HttpRequestException($"Gemini trả lỗi {(int)response.StatusCode}: {errorBody}");
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(GeminiJsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Không thể đọc phản hồi từ Gemini");

            var text = apiResponse.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
            var usage = apiResponse.UsageMetadata != null
                ? new ChatUsageDto
                {
                    PromptTokens = apiResponse.UsageMetadata.PromptTokenCount ?? 0,
                    CompletionTokens = apiResponse.UsageMetadata.CandidatesTokenCount ?? 0,
                    TotalTokens = (apiResponse.UsageMetadata.PromptTokenCount ?? 0) + (apiResponse.UsageMetadata.CandidatesTokenCount ?? 0)
                }
                : null;

            return new ChatResponseDto { Content = text, Usage = usage };
        }

        #region Gemini API Models

        private class GeminiRequest
        {
            public List<GeminiContent> Contents { get; set; } = new();
            public GeminiSystemInstruction? SystemInstruction { get; set; }
            public GeminiGenerationConfig? GenerationConfig { get; set; }
        }

        private class GeminiContent
        {
            public List<GeminiPart> Parts { get; set; } = new();
        }

        private class GeminiPart
        {
            public string Text { get; set; } = "";
        }

        private class GeminiSystemInstruction
        {
            public List<GeminiPart> Parts { get; set; } = new();
        }

        private class GeminiGenerationConfig
        {
            public int MaxOutputTokens { get; set; }
            public double Temperature { get; set; }
        }

        private class GeminiResponse
        {
            public List<GeminiCandidate>? Candidates { get; set; }
            public GeminiUsageMetadata? UsageMetadata { get; set; }
        }

        private class GeminiCandidate
        {
            public GeminiContent? Content { get; set; }
        }

        private class GeminiUsageMetadata
        {
            public int? PromptTokenCount { get; set; }
            public int? CandidatesTokenCount { get; set; }
        }

        #endregion
    }
}
