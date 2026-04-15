namespace BLL.Services.Ai;

/// <summary>
/// Upstream AI (vd. OpenRouter → Google) trả HTTP 429 — model free thường bị rate limit tạm thời.
/// </summary>
public sealed class AiRateLimitedException : Exception
{
    public AiRateLimitedException(string message) : base(message) { }

    public AiRateLimitedException(string message, Exception innerException) : base(message, innerException) { }
}
