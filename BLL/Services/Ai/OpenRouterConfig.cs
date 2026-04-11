using Microsoft.Extensions.Configuration;

namespace BLL.Services.Ai;

/// <summary>
/// Cấu hình OpenRouter (API tương thích OpenAI). Giống PRN232_NongSanWebsite_Backend.
/// Docs: https://openrouter.ai/docs
/// </summary>
public static class OpenRouterConfig
{
    public static string? ApiKey(IConfiguration c) => c["OpenRouter:ApiKey"];

    public static string BaseUrl(IConfiguration c) =>
        (c["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1").Trim().TrimEnd('/');

    /// <summary>Mặc định: Gemini 2.0 Flash (free) — nhanh, hỗ trợ ảnh (quét bill / OCR).</summary>
    public static string ModelId(IConfiguration c) =>
        c["OpenRouter:ModelId"] ?? "google/gemini-2.0-flash-exp:free";

    /// <summary>
    /// OpenRouter khuyến nghị gửi để hiển thị trên bảng xếp hạng (không bắt buộc).
    /// </summary>
    public static void ApplyOptionalHeaders(HttpRequestMessage req, IConfiguration c)
    {
        var referer = c["OpenRouter:HttpReferer"];
        if (!string.IsNullOrWhiteSpace(referer))
            req.Headers.TryAddWithoutValidation("HTTP-Referer", referer.Trim());

        var title = c["OpenRouter:SiteTitle"];
        if (!string.IsNullOrWhiteSpace(title))
            req.Headers.TryAddWithoutValidation("X-Title", title.Trim());
    }
}
