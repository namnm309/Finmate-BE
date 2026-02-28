using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FinmateController.Hubs
{
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

        /// <summary>
        /// Lấy UserId từ token với logic ưu tiên Clerk trước, fallback sang Basic JWT
        /// </summary>
        private async Task<Guid?> GetCurrentUserIdAsync()
        {
            // Priority 1: Clerk token - kiểm tra "sub" claim trước
            var clerkUserId = Context.User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(clerkUserId) && !Guid.TryParse(clerkUserId, out _))
            {
                // "sub" claim tồn tại và KHÔNG phải là Guid => đây là Clerk user ID
                var clerkUserDto = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
                if (clerkUserDto != null)
                {
                    return clerkUserDto.Id;
                }
            }

            // Priority 2: Basic JWT - kiểm tra NameIdentifier/userId claim (Guid)
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? Context.User?.FindFirst("userId")?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            // Priority 3: Clerk token với NameIdentifier (một số config Clerk dùng NameIdentifier thay vì sub)
            var nameIdentifier = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(nameIdentifier) && !Guid.TryParse(nameIdentifier, out _))
            {
                var clerkUserDto = await _userService.GetOrCreateUserFromClerkAsync(nameIdentifier);
                if (clerkUserDto != null)
                {
                    return clerkUserDto.Id;
                }
            }

            return null;
        }

        // Được client gọi sau khi connect để join vào group theo user.
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

