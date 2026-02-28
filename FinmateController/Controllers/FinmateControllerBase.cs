using BLL.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinmateController.Controllers
{
    /// <summary>
    /// Base controller cung cấp method GetCurrentUserIdAsync() với logic ưu tiên Clerk token
    /// </summary>
    public abstract class FinmateControllerBase : ControllerBase
    {
        protected readonly UserService _userService;

        protected FinmateControllerBase(UserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Lấy UserId từ token với logic ưu tiên Clerk trước, fallback sang Basic JWT
        /// </summary>
        /// <returns>User ID từ database hoặc null nếu không tìm thấy</returns>
        protected async Task<Guid?> GetCurrentUserIdAsync()
        {
            // Priority 1: Clerk token - kiểm tra "sub" claim trước
            var clerkUserId = User.FindFirst("sub")?.Value;
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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("userId")?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            // Priority 3: Clerk token với NameIdentifier (một số config Clerk dùng NameIdentifier thay vì sub)
            var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
    }
}
