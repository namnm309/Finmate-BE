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
| `OpenRouter__ModelId` | **Bắt buộc** (không còn trong `appsettings` repo). Ví dụ: `google/gemma-4-31b-it:free`. Nếu **No endpoints found**, thử `openrouter/free` hoặc model `:free` khác trên [Models (free)](https://openrouter.ai/models?pricing=free). |
| `OpenRouter__MaxTokens`, `OpenRouter__Temperature` | Tuỳ chọn |
| `OpenRouter__HttpReferer`, `OpenRouter__SiteTitle` | Tuỳ chọn (ranking OpenRouter). Trong repo: referer mặc định **`https://finmate.website`** (FE production). Azure có thể ghi đè `OpenRouter__HttpReferer` nếu đổi domain. |

Tài liệu API: [OpenRouter](https://openrouter.ai/docs).

## Kiểm tra

`GET /api/chat/diagnostic` — trường `provider` phản ánh provider đang dùng; trạng thái OK khi key và model hợp lệ.

## HTTP 429 / rate limit (model free)

Nếu log OpenRouter có `429` hoặc chữ **rate-limited upstream** (Google AI Studio): đó là **hết quota tạm thời** cho model free dùng chung, không phải bug backend. Cách xử lý: chờ vài phút; đổi **`OpenRouter__ModelId`** (vd. `openrouter/free`); nạp credit + dùng model trả phí; hoặc [BYOK Google trong OpenRouter](https://openrouter.ai/settings/integrations). API chat trả **HTTP 429** với `code: ai_rate_limited` để app hiển thị “thử lại sau”.

**Lưu ý:** Danh sách model vision (`GetVisionModels`) chỉ gọi MegaLLM khi provider là MegaLLM; với OpenRouter endpoint trả rỗng (có thể mở rộng sau nếu cần).
