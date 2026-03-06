using Microsoft.EntityFrameworkCore;

namespace FinmateController.Helpers
{
    /// <summary>
    /// Helper thống nhất xử lý lỗi API - thuận tiện debug sau khi deploy (push auto-deploy).
    /// Luôn trả message hữu ích cho client, log đầy đủ server-side.
    /// </summary>
    public static class ApiErrorHelper
    {
        /// <summary>
        /// Tạo response 500 với message hữu ích. Client sẽ hiển thị message để debug.
        /// </summary>
        public static object Build500Response(Exception ex, ILogger logger, string actionName, string? correlationId = null)
        {
            correlationId ??= Guid.NewGuid().ToString("N")[..8];
            logger.LogError(ex, "[{CorrelationId}] Error {Action}: {Message}", correlationId, actionName, ex.Message);

            var msg = ex switch
            {
                DbUpdateException dbEx => dbEx.InnerException?.Message ?? dbEx.Message,
                InvalidOperationException ioEx => ioEx.Message,
                _ => ex.InnerException?.Message ?? ex.Message
            };

            return new
            {
                message = msg ?? "Internal server error",
                error = msg ?? "Internal server error",
                correlationId
            };
        }
    }
}
