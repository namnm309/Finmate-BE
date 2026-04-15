namespace BLL.Services.Ai
{
    public sealed class AiQuotaExceededException : Exception
    {
        public AiQuotaExceededException(string message) : base(message) { }
    }
}
