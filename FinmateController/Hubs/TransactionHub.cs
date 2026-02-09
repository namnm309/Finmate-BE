using System.Security.Claims;
using System.Threading.Tasks;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FinmateController.Hubs
{
    /// <summary>
    /// SignalR hub dùng để push các thay đổi giao dịch theo từng user.
    /// Mỗi user sẽ join vào group "user:{userId}" để chỉ nhận event của chính mình.
    /// </summary>
    [Authorize]
    public class TransactionHub : Hub
    {
        private readonly UserService _userService;
        private readonly ILogger<TransactionHub> _logger;

        public TransactionHub(UserService userService, ILogger<TransactionHub> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        private async Task<Guid?> GetCurrentUserIdAsync()
        {
            var clerkUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? Context.User?.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(clerkUserId))
            {
                return null;
            }

            var user = await _userService.GetUserByClerkIdAsync(clerkUserId);
            return user?.Id;
        }

        /// <summary>
        /// Được client gọi sau khi connect để join vào group theo user.
        </summary>
        public async Task JoinUserGroup()
        {
            var userId = await GetCurrentUserIdAsync();
            if (!userId.HasValue)
            {
                _logger.LogWarning("JoinUserGroup called but user is not authenticated or not found.");
                return;
            }

            var groupName = $"user:{userId.Value}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Connection {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);
        }
    }
}

