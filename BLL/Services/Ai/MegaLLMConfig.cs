using Microsoft.Extensions.Configuration;

namespace BLL.Services.Ai;

/// <summary>
/// Cấu hình MegaLLM (OpenAI-compatible). Đối xứng với <see cref="OpenRouterConfig"/>.
/// Docs: https://docs.megallm.io/
/// </summary>
public static class MegaLLMConfig
{
    public static string? ApiKey(IConfiguration c) => c["MegaLLM:ApiKey"];

    public static string BaseUrl(IConfiguration c) =>
        (c["MegaLLM:BaseUrl"] ?? "https://ai.megallm.io/v1").Trim().TrimEnd('/');

    public static string ModelId(IConfiguration c) =>
        c["MegaLLM:ModelId"] ?? "openai-gpt-oss-20b";
}
