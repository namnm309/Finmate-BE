using BLL.DTOs.Response;
using DAL.Models;
using DAL.Repositories;

namespace BLL.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ClerkService _clerkService;

        public UserService(IUserRepository userRepository, ClerkService clerkService)
        {
            _userRepository = userRepository;
            _clerkService = clerkService;
        }

        /// <summary>
        /// Lấy hoặc tạo user từ Clerk ID
        /// </summary>
        public async Task<UserResponseDto?> GetOrCreateUserFromClerkAsync(string clerkUserId)
        {
            // Kiểm tra user đã tồn tại trong database chưa
            var user = await _userRepository.GetByClerkUserIdAsync(clerkUserId);
            
            if (user != null)
            {
                return MapToDto(user);
            }

            // Nếu chưa có, lấy từ Clerk API và tạo mới
            var clerkUser = await _clerkService.GetUserByIdAsync(clerkUserId);
            if (clerkUser == null)
            {
                return null;
            }

            // Tạo user mới
            var newUser = await CreateUserFromClerkAsync(clerkUser);
            return MapToDto(newUser);
        }

        /// <summary>
        /// Lấy user theo Clerk ID
        /// </summary>
        public async Task<UserResponseDto?> GetUserByClerkIdAsync(string clerkUserId)
        {
            var user = await _userRepository.GetByClerkUserIdAsync(clerkUserId);
            return user != null ? MapToDto(user) : null;
        }

        /// <summary>
        /// Lấy user theo ID
        /// </summary>
        public async Task<UserResponseDto?> GetUserByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user != null ? MapToDto(user) : null;
        }

        /// <summary>
        /// Tạo user từ Clerk info
        /// </summary>
        public async Task<Users> CreateUserFromClerkAsync(ClerkUserInfo clerkUser)
        {
            var user = new Users
            {
                ClerkUserId = clerkUser.Id,
                Email = clerkUser.EmailAddress ?? "",
                FullName = $"{clerkUser.FirstName ?? ""} {clerkUser.LastName ?? ""}".Trim(),
                PhoneNumber = clerkUser.PhoneNumber,
                AvatarUrl = clerkUser.ImageUrl,
                PasswordHash = "", // Clerk users không cần password
                IsActive = true,
                CreatedAt = clerkUser.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = clerkUser.UpdatedAt ?? DateTime.UtcNow,
                LastLoginAt = clerkUser.LastSignInAt
            };

            return await _userRepository.AddAsync(user);
        }

        /// <summary>
        /// Tạo user từ Clerk webhook data
        /// </summary>
        public async Task<Users> CreateUserFromWebhookAsync(ClerkWebhookData webhookData)
        {
            // Lấy email từ email addresses (lấy email đầu tiên đã verified hoặc email đầu tiên)
            var email = webhookData.EmailAddresses?.FirstOrDefault(e => e.Verified == true)?.EmailAddress
                ?? webhookData.EmailAddresses?.FirstOrDefault()?.EmailAddress
                ?? string.Empty;

            var user = new Users
            {
                ClerkUserId = webhookData.Id,
                Email = email,
                FullName = $"{webhookData.FirstName ?? ""} {webhookData.LastName ?? ""}".Trim(),
                PhoneNumber = webhookData.PhoneNumbers?.FirstOrDefault()?.PhoneNumber,
                AvatarUrl = webhookData.ImageUrl,
                PasswordHash = "", // Clerk users không cần password hash
                IsActive = true,
                CreatedAt = webhookData.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = webhookData.UpdatedAt ?? DateTime.UtcNow,
                LastLoginAt = webhookData.LastSignInAt
            };

            return await _userRepository.AddAsync(user);
        }

        /// <summary>
        /// Update user từ Clerk info
        /// </summary>
        public async Task<UserResponseDto?> UpdateUserFromClerkAsync(string clerkUserId, ClerkUserInfo clerkUser)
        {
            var user = await _userRepository.GetByClerkUserIdAsync(clerkUserId);
            if (user == null)
            {
                return null;
            }

            user.Email = clerkUser.EmailAddress ?? user.Email;
            user.FullName = $"{clerkUser.FirstName ?? ""} {clerkUser.LastName ?? ""}".Trim();
            user.PhoneNumber = clerkUser.PhoneNumber ?? user.PhoneNumber;
            user.AvatarUrl = clerkUser.ImageUrl ?? user.AvatarUrl;
            user.UpdatedAt = clerkUser.UpdatedAt ?? DateTime.UtcNow;
            user.LastLoginAt = clerkUser.LastSignInAt ?? user.LastLoginAt;

            var updatedUser = await _userRepository.UpdateAsync(user);
            return MapToDto(updatedUser);
        }

        /// <summary>
        /// Update user từ Clerk webhook data
        /// </summary>
        public async Task<UserResponseDto?> UpdateUserFromWebhookAsync(ClerkWebhookData webhookData)
        {
            var user = await _userRepository.GetByClerkUserIdAsync(webhookData.Id ?? "");
            if (user == null)
            {
                // Nếu không tìm thấy, tạo mới
                var newUser = await CreateUserFromWebhookAsync(webhookData);
                return MapToDto(newUser);
            }

            // Cập nhật thông tin user
            var email = webhookData.EmailAddresses?.FirstOrDefault(e => e.Verified == true)?.EmailAddress
                ?? webhookData.EmailAddresses?.FirstOrDefault()?.EmailAddress;

            if (!string.IsNullOrEmpty(email))
            {
                user.Email = email;
            }

            user.FullName = $"{webhookData.FirstName ?? ""} {webhookData.LastName ?? ""}".Trim();
            user.PhoneNumber = webhookData.PhoneNumbers?.FirstOrDefault()?.PhoneNumber ?? user.PhoneNumber;
            user.AvatarUrl = webhookData.ImageUrl ?? user.AvatarUrl;
            user.UpdatedAt = webhookData.UpdatedAt ?? DateTime.UtcNow;
            user.LastLoginAt = webhookData.LastSignInAt ?? user.LastLoginAt;

            var updatedUser = await _userRepository.UpdateAsync(user);
            return MapToDto(updatedUser);
        }

        /// <summary>
        /// Deactivate user
        /// </summary>
        public async Task<bool> DeactivateUserAsync(string clerkUserId)
        {
            var user = await _userRepository.GetByClerkUserIdAsync(clerkUserId);
            if (user == null)
            {
                return false;
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        /// <summary>
        /// Map Entity sang DTO
        /// </summary>
        private UserResponseDto MapToDto(Users user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                ClerkUserId = user.ClerkUserId,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                IsActive = user.IsActive,
                IsPremium = user.IsPremium,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}
