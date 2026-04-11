# Chuyển đổi AI: MegaLLM ↔ OpenRouter (Finmate-BE)

Backend dùng API **OpenAI-compatible** (cùng endpoint `POST …/chat/completions`). Chọn nhà cung cấp bằng một khóa cấu hình; code resolve tập trung trong `BLL/Services/Ai/` (tương tự pattern `OpenRouterConfig` ở PRN232).

## Chọn provider

| Cách | Giá trị |
|------|---------|
| `appsettings.json` | `"AISupport": { "Provider": "OpenRouter" }` hoặc `"MegaLLM"` |
| Azure | `AISupport__Provider` = `OpenRouter` hoặc `MegaLLM` |

Mặc định nếu **không** khai báo `AISupport:Provider`: **OpenRouter** (trùng với `AiProviderResolver`).

Trong repo, block **MegaLLM** trong `appsettings.json` đang được **comment JSON** (`/* … */`) để dùng OpenRouter; bật lại MegaLLM thì bỏ comment và đổi `Provider`.

## File code liên quan

| File | Vai trò |
|------|---------|
| `AiProviderResolver.cs` | Đọc `AISupport:Provider`, trả `ResolvedAiConnection` |
| `MegaLLMConfig.cs` | `ApiKey`, `BaseUrl`, `ModelId` cho MegaLLM |
| `OpenRouterConfig.cs` | Tương tự + `ApplyOptionalHeaders` (HTTP-Referer, X-Title) |
| `ChatService.cs` | Gọi `AiProviderResolver.Resolve`, gửi chat |

## Biến môi trường / Azure (dùng `__` trên Azure)

### Khi `AISupport__Provider` = `MegaLLM`

| Name | Ghi chú |
|------|---------|
| `MegaLLM__ApiKey` | Bắt buộc |
| `MegaLLM__BaseUrl` | Mặc định `https://ai.megallm.io/v1` |
| `MegaLLM__ModelId` | Mặc định theo appsettings |
| `MegaLLM__MaxTokens`, `MegaLLM__Temperature` | Tuỳ chọn |

### Khi `AISupport__Provider` = `OpenRouter`

| Name | Ghi chú |
|------|---------|
| `OpenRouter__ApiKey` | Bắt buộc — [openrouter.ai/keys](https://openrouter.ai/keys) |
| `OpenRouter__BaseUrl` | Mặc định `https://openrouter.ai/api/v1` |
| `OpenRouter__ModelId` | Mặc định dự án: `google/gemini-2.0-flash-exp:free` (miễn phí, nhanh, có vision — quét bill). Thay bằng model khác trên [Models](https://openrouter.ai/models), lọc **Image** + **Pricing**; ví dụ dự phòng: `meta-llama/llama-3.2-11b-vision-instruct:free` |
| `OpenRouter__MaxTokens`, `OpenRouter__Temperature` | Tuỳ chọn |
| `OpenRouter__HttpReferer`, `OpenRouter__SiteTitle` | Tuỳ chọn (ranking OpenRouter) |

Tài liệu API: [OpenRouter](https://openrouter.ai/docs).

## Kiểm tra

`GET /api/chat/diagnostic` — trường `provider` phản ánh provider đang dùng; trạng thái OK khi key và model hợp lệ.

**Lưu ý:** Danh sách model vision (`GetVisionModels`) chỉ gọi MegaLLM khi provider là MegaLLM; với OpenRouter endpoint trả rỗng (có thể mở rộng sau nếu cần).
