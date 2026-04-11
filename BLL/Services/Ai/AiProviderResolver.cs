using Microsoft.Extensions.Configuration;

namespace BLL.Services.Ai;

/// <summary>
/// Chọn MegaLLM hoặc OpenRouter theo <c>AISupport:Provider</c> — đổi 1 dòng config là chuyển provider.
/// </summary>
public static class AiProviderResolver
{
    public const string SectionName = "AISupport";
    public const string ProviderKey = "Provider";

    /// <summary>
    /// Giá trị hợp lệ: <c>OpenRouter</c> (mặc định khi không cấu hình), <c>MegaLLM</c>.
    /// </summary>
    public static ResolvedAiConnection Resolve(IConfiguration configuration)
    {
        var provider = (configuration[$"{SectionName}:{ProviderKey}"] ?? "OpenRouter").Trim();

        if (string.Equals(provider, "OpenRouter", StringComparison.OrdinalIgnoreCase))
        {
            return new ResolvedAiConnection(
                AiProviderKind.OpenRouter,
                "OpenRouter",
                OpenRouterConfig.ApiKey(configuration) ?? "",
                OpenRouterConfig.BaseUrl(configuration),
                OpenRouterConfig.ModelId(configuration),
                configuration.GetValue<int>("OpenRouter:MaxTokens", 8192),
                configuration.GetValue<double>("OpenRouter:Temperature", 0.4));
        }

        return new ResolvedAiConnection(
            AiProviderKind.MegaLLM,
            "MegaLLM",
            MegaLLMConfig.ApiKey(configuration) ?? "",
            MegaLLMConfig.BaseUrl(configuration),
            MegaLLMConfig.ModelId(configuration),
            configuration.GetValue<int>("MegaLLM:MaxTokens", 4096),
            configuration.GetValue<double>("MegaLLM:Temperature", 0.7));
    }

    public static string AzureApiKeyEnvHint(AiProviderKind kind) =>
        kind == AiProviderKind.OpenRouter ? "OpenRouter__ApiKey" : "MegaLLM__ApiKey";

    public static string AzureModelIdEnvHint(AiProviderKind kind) =>
        kind == AiProviderKind.OpenRouter ? "OpenRouter__ModelId" : "MegaLLM__ModelId";
}
