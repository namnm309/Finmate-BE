using Microsoft.Extensions.Configuration;

namespace BLL.Services.Ai;

/// <summary>
/// Thông tin kết nối tới upstream (MegaLLM hoặc OpenRouter).
/// </summary>
public sealed record ResolvedAiConnection(
    AiProviderKind Kind,
    string DisplayName,
    string ApiKey,
    string BaseUrl,
    string DefaultModelId,
    int MaxTokens,
    double Temperature)
{
    /// <summary>
    /// OpenRouter: gửi HTTP-Referer / X-Title theo docs. MegaLLM: không thêm header.
    /// </summary>
    public void ApplyOptionalProviderHeaders(HttpRequestMessage request, IConfiguration configuration)
    {
        if (Kind == AiProviderKind.OpenRouter)
            OpenRouterConfig.ApplyOptionalHeaders(request, configuration);
    }
}
