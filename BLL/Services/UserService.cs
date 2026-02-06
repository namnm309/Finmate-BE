using BLL.DTOs.Request;
using BLL.DTOs.Response;
using DAL.Models;
using DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace BLL.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ClerkService _clerkService;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, ClerkService clerkService, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _clerkService = clerkService;
            _logger = logger;
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
            // Đảm bảo FullName không rỗng
            var fullName = $"{clerkUser.FirstName ?? ""} {clerkUser.LastName ?? ""}".Trim();
            if (string.IsNullOrEmpty(fullName))
            {
                fullName = clerkUser.EmailAddress?.Split('@')[0] ?? "User";
            }

            // Đảm bảo Email không bao giờ empty string - dùng placeholder nếu không có
            var email = string.IsNullOrWhiteSpace(clerkUser.EmailAddress)
                ? $"user_{clerkUser.Id?.Replace("user_", "") ?? Guid.NewGuid().ToString()}@placeholder.local"
                : clerkUser.EmailAddress;

            var user = new Users
            {
                ClerkUserId = clerkUser.Id,
                Email = email,
                FullName = fullName,
                PhoneNumber = clerkUser.PhoneNumber ?? "",
                AvatarUrl = clerkUser.ImageUrl ?? "",
                PasswordHash = "", // Clerk users không cần password
                IsActive = true,
                CreatedAt = clerkUser.GetCreatedAtDateTime() ?? DateTime.UtcNow,
                UpdatedAt = clerkUser.GetUpdatedAtDateTime() ?? DateTime.UtcNow,
                LastLoginAt = clerkUser.GetLastSignInAtDateTime()
            };

            // Validate entity trước khi save
            ValidateUserEntity(user, "CreateUserFromClerkAsync");

            _logger.LogInformation("Attempting to save user with ClerkId: {ClerkId}, Email: {Email}, FullName: {FullName}", 
                user.ClerkUserId, user.Email, user.FullName);

            try
            {
                var savedUser = await _userRepository.AddAsync(user);
                _logger.LogInformation("Successfully saved user with Id: {Id}, ClerkId: {ClerkId}, Email: {Email}", 
                    savedUser.Id, savedUser.ClerkUserId, savedUser.Email);
                return savedUser;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error saving user. ClerkId: {ClerkId}, Email: {Email}, FullName: {FullName}. Error: {Error}", 
                    user.ClerkUserId, user.Email, user.FullName, dbEx.Message);
                throw new InvalidOperationException(
                    $"Database error saving user: {dbEx.Message}. Inner: {dbEx.InnerException?.Message}", 
                    dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user to database. ClerkId: {ClerkId}, Email: {Email}, FullName: {FullName}. Error: {Error}", 
                    user.ClerkUserId, user.Email, user.FullName, ex.Message);
                throw new InvalidOperationException(
                    $"Error saving user to database: {ex.Message}", 
                    ex);
            }
        }

        /// <summary>
        /// Lấy email từ webhook data; nếu webhook không có thì gọi Clerk Backend API để lấy user (email).
        /// Webhook Clerk có thể không gửi kèm email_addresses đầy đủ nên cần fallback API.
        /// </summary>
        private async Task<string?> GetEmailFromWebhookOrClerkApiAsync(ClerkWebhookData webhookData)
        {
            var email = webhookData.EmailAddresses?.FirstOrDefault(e => e.IsVerified)?.EmailAddress
                ?? webhookData.EmailAddresses?.FirstOrDefault()?.EmailAddress;
            if (!string.IsNullOrWhiteSpace(email))
                return email;

            if (string.IsNullOrEmpty(webhookData.Id))
                return null;

            var clerkUser = await _clerkService.GetUserByIdAsync(webhookData.Id);
            email = clerkUser?.GetPrimaryEmail();
            if (!string.IsNullOrWhiteSpace(email))
                _logger.LogInformation("Retrieved email from Clerk API for ClerkId {ClerkId}: {Email}", webhookData.Id, email);
            return string.IsNullOrWhiteSpace(email) ? null : email;
        }

        /// <summary>
        /// Tạo user từ Clerk webhook data
        /// </summary>
        public async Task<Users> CreateUserFromWebhookAsync(ClerkWebhookData webhookData)
        {
            var email = await GetEmailFromWebhookOrClerkApiAsync(webhookData);

            // Chỉ dùng placeholder khi thật sự không có email (kể cả từ Clerk API)
            if (string.IsNullOrWhiteSpace(email))
            {
                var clerkUserId = webhookData.Id ?? Guid.NewGuid().ToString();
                email = $"user_{clerkUserId.Replace("user_", "")}@placeholder.local";
                _logger.LogWarning("No email in webhook or Clerk API for ClerkId: {ClerkId}, using placeholder: {Email}", 
                    clerkUserId, email);
            }

            // Đảm bảo FullName không rỗng
            var fullName = $"{webhookData.FirstName ?? ""} {webhookData.LastName ?? ""}".Trim();
            if (string.IsNullOrEmpty(fullName))
            {
                fullName = email?.Split('@')[0] ?? "User";
            }

            var user = new Users
            {
                ClerkUserId = webhookData.Id ?? "",
                Email = email,
                FullName = fullName,
                PhoneNumber = webhookData.PhoneNumbers?.FirstOrDefault()?.PhoneNumber ?? "",
                AvatarUrl = webhookData.ImageUrl ?? "",
                PasswordHash = "", // Clerk users không cần password hash
                IsActive = true,
                CreatedAt = webhookData.GetCreatedAtDateTime() ?? DateTime.UtcNow,
                UpdatedAt = webhookData.GetUpdatedAtDateTime() ?? DateTime.UtcNow,
                LastLoginAt = webhookData.GetLastSignInAtDateTime()
            };

            // Validate entity trước khi save
            ValidateUserEntity(user, "CreateUserFromWebhookAsync");

            _logger.LogInformation("Attempting to save user from webhook with ClerkId: {ClerkId}, Email: {Email}, FullName: {FullName}", 
                user.ClerkUserId, user.Email, user.FullName);

            try
            {
                var savedUser = await _userRepository.AddAsync(user);
                _logger.LogInformation("Successfully saved user from webhook with Id: {Id}, ClerkId: {ClerkId}, Email: {Email}", 
                    savedUser.Id, savedUser.ClerkUserId, savedUser.Email);
                return savedUser;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error saving user from webhook. ClerkId: {ClerkId}, Email: {Email}, FullName: {FullName}. Error: {Error}", 
                    user.ClerkUserId, user.Email, user.FullName, dbEx.Message);
                throw new InvalidOperationException(
                    $"Database error saving user: {dbEx.Message}. Inner: {dbEx.InnerException?.Message}", 
                    dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user from webhook to database. ClerkId: {ClerkId}, Email: {Email}, FullName: {FullName}. Error: {Error}", 
                    user.ClerkUserId, user.Email, user.FullName, ex.Message);
                throw new InvalidOperationException(
                    $"Error saving user to database: {ex.Message}", 
                    ex);
            }
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
            
            // Đảm bảo FullName không rỗng
            var fullName = $"{clerkUser.FirstName ?? ""} {clerkUser.LastName ?? ""}".Trim();
            if (!string.IsNullOrEmpty(fullName))
            {
                user.FullName = fullName;
            }
            
            user.PhoneNumber = clerkUser.PhoneNumber ?? user.PhoneNumber ?? "";
            user.AvatarUrl = clerkUser.ImageUrl ?? user.AvatarUrl ?? "";
            user.UpdatedAt = clerkUser.GetUpdatedAtDateTime() ?? DateTime.UtcNow;
            user.LastLoginAt = clerkUser.GetLastSignInAtDateTime() ?? user.LastLoginAt;

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
            var email = webhookData.EmailAddresses?.FirstOrDefault(e => e.IsVerified)?.EmailAddress
                ?? webhookData.EmailAddresses?.FirstOrDefault()?.EmailAddress;

            if (!string.IsNullOrEmpty(email))
            {
                user.Email = email;
            }

            // Đảm bảo FullName không rỗng
            var fullName = $"{webhookData.FirstName ?? ""} {webhookData.LastName ?? ""}".Trim();
            if (!string.IsNullOrEmpty(fullName))
            {
                user.FullName = fullName;
            }
            
            user.PhoneNumber = webhookData.PhoneNumbers?.FirstOrDefault()?.PhoneNumber ?? user.PhoneNumber ?? "";
            user.AvatarUrl = webhookData.ImageUrl ?? user.AvatarUrl ?? "";
            user.UpdatedAt = webhookData.GetUpdatedAtDateTime() ?? DateTime.UtcNow;
            user.LastLoginAt = webhookData.GetLastSignInAtDateTime() ?? user.LastLoginAt;

            var updatedUser = await _userRepository.UpdateAsync(user);
            return MapToDto(updatedUser);
        }

        /// <summary>
        /// Cập nhật thông tin profile của user
        /// </summary>
        public async Task<UserResponseDto?> UpdateUserProfileAsync(string clerkUserId, UpdateUserRequestDto request)
        {
            var user = await _userRepository.GetByClerkUserIdAsync(clerkUserId);
            if (user == null)
            {
                return null;
            }

            // Chỉ cập nhật các field được cung cấp (không null)
            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                user.FullName = request.FullName;
            }

            if (request.PhoneNumber != null)
            {
                user.PhoneNumber = request.PhoneNumber;
            }

            if (request.Address != null)
            {
                user.Address = request.Address;
            }

            if (request.DateOfBirth.HasValue)
            {
                user.DateOfBirth = request.DateOfBirth;
            }

            if (request.Occupation != null)
            {
                user.Occupation = request.Occupation;
            }

            if (request.AvatarUrl != null)
            {
                user.AvatarUrl = request.AvatarUrl;
            }

            user.UpdatedAt = DateTime.UtcNow;

            var updatedUser = await _userRepository.UpdateAsync(user);
            return MapToDto(updatedUser);
        }

        /// <summary>
        /// Xóa tất cả dữ liệu của user (hiện tại chỉ reset profile, sẽ mở rộng khi có các bảng liên quan)
        /// </summary>
        public async Task<bool> DeleteUserDataAsync(string clerkUserId)
        {
            var user = await _userRepository.GetByClerkUserIdAsync(clerkUserId);
            if (user == null)
            {
                return false;
            }

            // Reset các thông tin profile về null/empty
            user.FullName = "";
            user.PhoneNumber = "";
            user.Address = null;
            user.DateOfBirth = null;
            user.Occupation = null;
            user.AvatarUrl = "";
            user.UpdatedAt = DateTime.UtcNow;

            // TODO: Khi có các bảng liên quan, thêm logic xóa:
            // - Transactions
            // - Categories
            // - Goals
            // - Budgets

            await _userRepository.UpdateAsync(user);
            return true;
        }

        /// <summary>
        /// Xóa tài khoản (soft delete)
        /// </summary>
        public async Task<bool> DeleteUserAccountAsync(string clerkUserId)
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
        /// Chuẩn hóa email để tra cứu (trim, lowercase). Trả về null nếu null/empty.
        /// </summary>
        private static string? NormalizeEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return email.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Email placeholder từ webhook (không dùng để gộp tài khoản theo email).
        /// </summary>
        private static bool IsPlaceholderEmail(string? email)
        {
            return !string.IsNullOrEmpty(email) &&
                   email.Trim().EndsWith("@placeholder.local", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Cập nhật bản ghi user đã tồn tại bằng dữ liệu webhook (link ClerkId mới, cập nhật thông tin).
        /// Không thay đổi Id, CreatedAt; không ghi đè PasswordHash nếu user đã có.
        /// </summary>
        public async Task<UserResponseDto?> UpdateExistingUserFromWebhookAsync(Users existingUser, ClerkWebhookData webhookData)
        {
            existingUser.ClerkUserId = webhookData.Id ?? existingUser.ClerkUserId;

            var email = await GetEmailFromWebhookOrClerkApiAsync(webhookData);
            if (!string.IsNullOrWhiteSpace(email) && !IsPlaceholderEmail(email))
            {
                existingUser.Email = email;
            }

            var fullName = $"{webhookData.FirstName ?? ""} {webhookData.LastName ?? ""}".Trim();
            if (!string.IsNullOrEmpty(fullName))
            {
                existingUser.FullName = fullName;
            }

            existingUser.PhoneNumber = webhookData.PhoneNumbers?.FirstOrDefault()?.PhoneNumber ?? existingUser.PhoneNumber ?? "";
            existingUser.AvatarUrl = webhookData.ImageUrl ?? existingUser.AvatarUrl ?? "";
            existingUser.UpdatedAt = webhookData.GetUpdatedAtDateTime() ?? DateTime.UtcNow;
            existingUser.LastLoginAt = webhookData.GetLastSignInAtDateTime() ?? existingUser.LastLoginAt;

            var updatedUser = await _userRepository.UpdateAsync(existingUser);
            return MapToDto(updatedUser);
        }

        /// <summary>
        /// Upsert user từ webhook: nếu đã có theo ClerkId thì cập nhật; nếu chưa có thì tìm theo email,
        /// nếu có user cùng email thì cập nhật và gán ClerkUserId mới; ngược lại tạo mới.
        /// </summary>
        public async Task<UserResponseDto?> UpsertUserFromWebhookAsync(ClerkWebhookData webhookData)
        {
            if (string.IsNullOrEmpty(webhookData.Id))
            {
                _logger.LogWarning("UpsertUserFromWebhookAsync: webhook data missing Id");
                return null;
            }

            var existingByClerkId = await _userRepository.GetByClerkUserIdAsync(webhookData.Id);
            if (existingByClerkId != null)
            {
                var updated = await UpdateUserFromWebhookAsync(webhookData);
                _logger.LogInformation("UpsertUserFromWebhookAsync: updated existing user by ClerkId {ClerkId}", webhookData.Id);
                return updated;
            }

            var email = await GetEmailFromWebhookOrClerkApiAsync(webhookData);
            var normalizedEmail = NormalizeEmail(email);

            if (string.IsNullOrEmpty(normalizedEmail) || (email != null && IsPlaceholderEmail(email)))
            {
                var newUser = await CreateUserFromWebhookAsync(webhookData);
                _logger.LogInformation("UpsertUserFromWebhookAsync: created new user (no real email) with ClerkId {ClerkId}", webhookData.Id);
                return MapToDto(newUser);
            }

            var existingByEmail = await _userRepository.GetByEmailNormalizedAsync(normalizedEmail);
            if (existingByEmail != null)
            {
                var updated = await UpdateExistingUserFromWebhookAsync(existingByEmail, webhookData);
                _logger.LogInformation("UpsertUserFromWebhookAsync: linked existing user by email {Email} to ClerkId {ClerkId}", normalizedEmail, webhookData.Id);
                return updated;
            }

            var created = await CreateUserFromWebhookAsync(webhookData);
            _logger.LogInformation("UpsertUserFromWebhookAsync: created new user with ClerkId {ClerkId}, email {Email}", webhookData.Id, normalizedEmail);
            return MapToDto(created);
        }

        /// <summary>
        /// Validate user entity trước khi save
        /// </summary>
        private void ValidateUserEntity(Users user, string methodName)
        {
            var validationErrors = new List<string>();

            // Validate Email
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                validationErrors.Add("Email is required and cannot be empty");
            }
            else if (!new EmailAddressAttribute().IsValid(user.Email))
            {
                validationErrors.Add($"Email '{user.Email}' is not in a valid format");
            }

            // Validate FullName
            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                validationErrors.Add("FullName is required and cannot be empty");
            }

            // Validate PasswordHash (có thể là empty string cho Clerk users)
            if (user.PasswordHash == null)
            {
                validationErrors.Add("PasswordHash cannot be null (can be empty string for Clerk users)");
            }

            if (validationErrors.Any())
            {
                var errorMessage = $"Validation failed in {methodName}: {string.Join("; ", validationErrors)}";
                _logger.LogError("User validation failed. ClerkId: {ClerkId}, Errors: {Errors}", 
                    user.ClerkUserId, errorMessage);
                throw new ArgumentException(errorMessage);
            }
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
                Address = user.Address,
                Occupation = user.Occupation,
                DateOfBirth = user.DateOfBirth,
                IsActive = user.IsActive,
                IsPremium = user.IsPremium,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}
