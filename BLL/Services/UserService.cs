using BLL.DTOs.Request;
using BLL.DTOs.Response;
using DAL.Models;
using DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace BLL.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ClerkService _clerkService;
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _configuration;

        public UserService(
            IUserRepository userRepository,
            ClerkService clerkService,
            ILogger<UserService> logger,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _clerkService = clerkService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Lấy hoặc tạo user từ Clerk ID.
        /// Ưu tiên tìm theo ClerkId, sau đó merge theo Email nếu đã có user với email đó.
        /// </summary>
        public async Task<UserResponseDto?> GetOrCreateUserFromClerkAsync(string clerkUserId)
        {
            // Bước 1: Kiểm tra user đã tồn tại theo ClerkUserId
            var user = await _userRepository.GetByClerkUserIdAsync(clerkUserId);
            
            if (user != null)
            {
                _logger.LogInformation("[Auth] GetOrCreateUserFromClerk: Found existing user ClerkId={ClerkId}, UserId={UserId}", clerkUserId, user.Id);
                user = await MaybePromoteExistingUserAsync(user);
                return MapToDto(user);
            }

            // Bước 2: Không tìm thấy theo ClerkId, lấy thông tin từ Clerk API
            _logger.LogInformation("[Auth] GetOrCreateUserFromClerk: User NOT found for ClerkId={ClerkId}, checking Clerk API", clerkUserId);
            var clerkUser = await _clerkService.GetUserByIdAsync(clerkUserId);
            if (clerkUser == null)
            {
                _logger.LogWarning("[Auth] GetOrCreateUserFromClerk: Clerk user not found for ClerkId={ClerkId}", clerkUserId);
                return null;
            }

            // Bước 3: Lấy email từ Clerk user và chuẩn hóa
            var primaryEmail = clerkUser.GetPrimaryEmail();
            var normalizedEmail = NormalizeEmail(primaryEmail);

            // Bước 4: Nếu có email hợp lệ (không phải placeholder), tìm user theo email
            if (!string.IsNullOrEmpty(normalizedEmail) && !IsPlaceholderEmail(primaryEmail))
            {
                var existingUserByEmail = await _userRepository.GetByEmailNormalizedAsync(normalizedEmail);
                
                if (existingUserByEmail != null)
                {
                    // Merge: Cập nhật ClerkUserId và thông tin khác vào user hiện có
                    _logger.LogWarning(
                        "[Auth] GetOrCreateUserFromClerk: Merge user by email. ExistingUserId={UserId}, OldClerkId={OldClerkId}, NewClerkId={NewClerkId}, Email={Email}",
                        existingUserByEmail.Id, 
                        existingUserByEmail.ClerkUserId ?? "(null)", 
                        clerkUserId, 
                        normalizedEmail
                    );

                    existingUserByEmail.ClerkUserId = clerkUserId;
                    
                    // Cập nhật thông tin từ Clerk nếu có
                    if (!string.IsNullOrWhiteSpace(primaryEmail))
                    {
                        existingUserByEmail.Email = primaryEmail;
                    }

                    var fullName = $"{clerkUser.FirstName ?? ""} {clerkUser.LastName ?? ""}".Trim();
                    if (!string.IsNullOrEmpty(fullName))
                    {
                        existingUserByEmail.FullName = fullName;
                    }

                    existingUserByEmail.PhoneNumber = clerkUser.PhoneNumber ?? existingUserByEmail.PhoneNumber ?? "";
                    existingUserByEmail.AvatarUrl = clerkUser.ImageUrl ?? existingUserByEmail.AvatarUrl ?? "";
                    existingUserByEmail.UpdatedAt = clerkUser.GetUpdatedAtDateTime() ?? DateTime.UtcNow;
                    existingUserByEmail.LastLoginAt = clerkUser.GetLastSignInAtDateTime() ?? existingUserByEmail.LastLoginAt;

                    await ApplyBootstrapRoleAsync(existingUserByEmail);

                    var updatedUser = await _userRepository.UpdateAsync(existingUserByEmail);
                    return MapToDto(updatedUser);
                }
            }

            // Bước 5: Không có user theo ClerkId và không có user theo Email, tạo mới
            _logger.LogInformation("[Auth] GetOrCreateUserFromClerk: Creating new user for ClerkId={ClerkId}, Email={Email}", clerkUserId, normalizedEmail ?? "(no email)");
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
        /// Tạo user từ Clerk info.
        /// Kiểm tra lần cuối cùng xem user đã tồn tại chưa (race condition protection).
        /// </summary>
        public async Task<Users> CreateUserFromClerkAsync(ClerkUserInfo clerkUser)
        {
            // Kiểm tra lần cuối cùng trước khi tạo mới (race condition protection)
            if (!string.IsNullOrWhiteSpace(clerkUser.Id))
            {
                var existingByClerkId = await _userRepository.GetByClerkUserIdAsync(clerkUser.Id);
                if (existingByClerkId != null)
                {
                    _logger.LogWarning(
                        "[Auth] CreateUserFromClerk: Race condition detected - user already exists by ClerkId={ClerkId}. Returning existing user.",
                        clerkUser.Id
                    );
                    return existingByClerkId;
                }
            }

            var email = clerkUser.GetPrimaryEmail();
            var normalizedEmail = NormalizeEmail(email);

            if (!string.IsNullOrEmpty(normalizedEmail) && !string.IsNullOrWhiteSpace(email) && 
                !email.EndsWith("@placeholder.local", StringComparison.OrdinalIgnoreCase))
            {
                var existingByEmail = await _userRepository.GetByEmailNormalizedAsync(normalizedEmail);
                if (existingByEmail != null)
                {
                    _logger.LogWarning(
                        "[Auth] CreateUserFromClerk: Race condition detected - user already exists by Email={Email}. Merging ClerkId.",
                        normalizedEmail
                    );
                    
                    // Merge ClerkId vào user hiện có
                    existingByEmail.ClerkUserId = clerkUser.Id;
                    
                    var fullName = $"{clerkUser.FirstName ?? ""} {clerkUser.LastName ?? ""}".Trim();
                    if (!string.IsNullOrEmpty(fullName))
                    {
                        existingByEmail.FullName = fullName;
                    }
                    
                    existingByEmail.PhoneNumber = clerkUser.PhoneNumber ?? existingByEmail.PhoneNumber ?? "";
                    existingByEmail.AvatarUrl = clerkUser.ImageUrl ?? existingByEmail.AvatarUrl ?? "";
                    existingByEmail.UpdatedAt = clerkUser.GetUpdatedAtDateTime() ?? DateTime.UtcNow;
                    existingByEmail.LastLoginAt = clerkUser.GetLastSignInAtDateTime() ?? existingByEmail.LastLoginAt;
                    
                    await _userRepository.UpdateAsync(existingByEmail);
                    return existingByEmail;
                }
            }

            // Đảm bảo FullName không rỗng
            var fullNameNew = $"{clerkUser.FirstName ?? ""} {clerkUser.LastName ?? ""}".Trim();
            if (string.IsNullOrEmpty(fullNameNew))
            {
                fullNameNew = clerkUser.GetPrimaryEmail()?.Split('@')[0] ?? "User";
            }

            // Đảm bảo Email không bao giờ empty string - dùng placeholder nếu không có
            if (string.IsNullOrWhiteSpace(email))
            {
                var clerkUserId = clerkUser.Id ?? Guid.NewGuid().ToString();
                email = $"user_{clerkUserId.Replace("user_", "")}@placeholder.local";
                _logger.LogWarning("No email found for ClerkId: {ClerkId}, using placeholder: {Email}", 
                    clerkUserId, email);
            }

            var user = new Users
            {
                ClerkUserId = clerkUser.Id,
                Email = email,
                FullName = fullNameNew,
                PhoneNumber = clerkUser.PhoneNumber ?? "",
                AvatarUrl = clerkUser.ImageUrl ?? "",
                PasswordHash = "", // Clerk users không cần password
                IsActive = true,
                Role = Role.User,
                CreatedAt = clerkUser.GetCreatedAtDateTime() ?? DateTime.UtcNow,
                UpdatedAt = clerkUser.GetUpdatedAtDateTime() ?? DateTime.UtcNow,
                LastLoginAt = clerkUser.GetLastSignInAtDateTime()
            };

            await ApplyBootstrapRoleAsync(user);

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
                
                // Nếu lỗi unique constraint, thử tìm lại user đã tồn tại
                if (dbEx.InnerException?.Message?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
                    dbEx.InnerException?.Message?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logger.LogWarning("Unique constraint violation, attempting to find existing user");
                    
                    if (!string.IsNullOrWhiteSpace(clerkUser.Id))
                    {
                        var existing = await _userRepository.GetByClerkUserIdAsync(clerkUser.Id);
                        if (existing != null)
                        {
                            _logger.LogInformation("Found existing user by ClerkId after constraint violation");
                            return existing;
                        }
                    }
                }
                
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
        /// Bootstrap role cho môi trường dev:
        /// - Nếu DB chưa có user nào => user đầu tiên sẽ là Admin.
        /// - Nếu có allowlist theo email qua config thì áp dụng (Admin > Staff).
        /// </summary>
        private async Task ApplyBootstrapRoleAsync(Users user)
        {
            try
            {
                // Default: keep Role.User (0). Only promote if explicitly configured.
                // Optional: promote very first user to Admin (dev bootstrapping).
                var firstUserAdminEnabled =
                    string.Equals(_configuration["Bootstrap:FirstUserAdmin"], "true", StringComparison.OrdinalIgnoreCase);
                if (firstUserAdminEnabled)
                {
                    var hasAnyUser = await _userRepository.AnyAsync();
                    if (!hasAnyUser)
                    {
                        user.Role = Role.Admin;
                        _logger.LogWarning("[Auth] BootstrapRole: First user promoted to Admin. Email={Email}", user.Email);
                        return;
                    }
                }

                // 2) Optional allowlist by email (comma-separated)
                var email = (user.Email ?? "").Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(email)) return;

                // 0) Optional: auto-promote all signed-in Clerk users to Staff in dev
                // (useful for local dashboard bootstrapping)
                var autoPromoteAllToStaff =
                    string.Equals(_configuration["Bootstrap:AutoPromoteAllClerkUsersToStaff"], "true", StringComparison.OrdinalIgnoreCase);
                if (autoPromoteAllToStaff && user.Role == Role.User)
                {
                    user.Role = Role.Staff;
                    _logger.LogWarning("[Auth] BootstrapRole: Auto-promoted to Staff. Email={Email}", user.Email);
                    // Continue to allowlist checks (Admin can override)
                }

                var adminList = (_configuration["Bootstrap:AdminEmails"] ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => x.Trim().ToLowerInvariant())
                    .ToHashSet();

                var staffList = (_configuration["Bootstrap:StaffEmails"] ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => x.Trim().ToLowerInvariant())
                    .ToHashSet();

                if (adminList.Contains(email))
                {
                    user.Role = Role.Admin;
                    _logger.LogWarning("[Auth] BootstrapRole: Email allowlisted as Admin. Email={Email}", user.Email);
                    return;
                }

                if (staffList.Contains(email))
                {
                    user.Role = Role.Staff;
                    _logger.LogWarning("[Auth] BootstrapRole: Email allowlisted as Staff. Email={Email}", user.Email);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Auth] BootstrapRole: skipped due to error");
            }
        }

        private async Task<Users> MaybePromoteExistingUserAsync(Users user)
        {
            var before = user.Role;
            await ApplyBootstrapRoleAsync(user);

            if (user.Role != before)
            {
                user.UpdatedAt = DateTime.UtcNow;
                _logger.LogWarning("[Auth] BootstrapRole: Promoted existing user. UserId={UserId} Role={Role}", user.Id, user.Role);
                return await _userRepository.UpdateAsync(user);
            }

            return user;
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
        /// Tạo user từ Clerk webhook data.
        /// Kiểm tra lần cuối cùng xem user đã tồn tại chưa (race condition protection).
        /// </summary>
        public async Task<Users> CreateUserFromWebhookAsync(ClerkWebhookData webhookData)
        {
            if (webhookData == null)
            {
                throw new ArgumentNullException(nameof(webhookData));
            }

            var email = await GetEmailFromWebhookOrClerkApiAsync(webhookData);
            var normalizedEmail = NormalizeEmail(email);

            // Kiểm tra lần cuối cùng trước khi tạo mới (race condition protection)
            if (!string.IsNullOrWhiteSpace(webhookData.Id))
            {
                var existingByClerkId = await _userRepository.GetByClerkUserIdAsync(webhookData.Id);
                if (existingByClerkId != null)
                {
                    _logger.LogWarning(
                        "[Webhook] CreateUserFromWebhook: Race condition detected - user already exists by ClerkId={ClerkId}. Updating instead.",
                        webhookData.Id
                    );
                    return (await UpdateExistingUserFromWebhookAsync(existingByClerkId, webhookData)) != null 
                        ? existingByClerkId 
                        : throw new InvalidOperationException("Failed to update existing user");
                }
            }

            if (!string.IsNullOrEmpty(normalizedEmail) && !IsPlaceholderEmail(email))
            {
                var existingByEmail = await _userRepository.GetByEmailNormalizedAsync(normalizedEmail);
                if (existingByEmail != null)
                {
                    _logger.LogWarning(
                        "[Webhook] CreateUserFromWebhook: Race condition detected - user already exists by Email={Email}. Merging ClerkId.",
                        normalizedEmail
                    );
                    return (await UpdateExistingUserFromWebhookAsync(existingByEmail, webhookData)) != null 
                        ? existingByEmail 
                        : throw new InvalidOperationException("Failed to merge existing user");
                }
            }

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
                Role = Role.User,
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
                
                // Nếu lỗi unique constraint, thử tìm lại user đã tồn tại
                if (dbEx.InnerException?.Message?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
                    dbEx.InnerException?.Message?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logger.LogWarning("Unique constraint violation, attempting to find existing user");
                    
                    if (!string.IsNullOrWhiteSpace(webhookData.Id))
                    {
                        var existing = await _userRepository.GetByClerkUserIdAsync(webhookData.Id);
                        if (existing != null)
                        {
                            _logger.LogInformation("Found existing user by ClerkId after constraint violation");
                            return existing;
                        }
                    }
                }
                
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

            var primaryEmail = clerkUser.GetPrimaryEmail();
            if (!string.IsNullOrWhiteSpace(primaryEmail))
            {
                user.Email = primaryEmail;
            }
            
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
        /// Update user từ Clerk webhook data.
        /// Nếu không tìm thấy theo ClerkId, sẽ tìm theo Email và merge thay vì tạo mới.
        /// </summary>
        public async Task<UserResponseDto?> UpdateUserFromWebhookAsync(ClerkWebhookData webhookData)
        {
            var user = await _userRepository.GetByClerkUserIdAsync(webhookData.Id ?? "");
            if (user == null)
            {
                // Không tìm thấy theo ClerkId, kiểm tra theo Email
                var email = await GetEmailFromWebhookOrClerkApiAsync(webhookData);
                var normalizedEmail = NormalizeEmail(email);

                if (!string.IsNullOrEmpty(normalizedEmail) && !IsPlaceholderEmail(email))
                {
                    var existingUserByEmail = await _userRepository.GetByEmailNormalizedAsync(normalizedEmail);
                    
                    if (existingUserByEmail != null)
                    {
                        // Merge: Cập nhật ClerkUserId vào user hiện có
                        _logger.LogWarning(
                            "[Webhook] UpdateUserFromWebhook: Merge user by email. ExistingUserId={UserId}, OldClerkId={OldClerkId}, NewClerkId={NewClerkId}, Email={Email}",
                            existingUserByEmail.Id,
                            existingUserByEmail.ClerkUserId ?? "(null)",
                            webhookData.Id,
                            normalizedEmail
                        );

                        return await UpdateExistingUserFromWebhookAsync(existingUserByEmail, webhookData);
                    }
                }

                // Nếu không tìm thấy theo ClerkId và Email, tạo mới
                _logger.LogInformation("[Webhook] UpdateUserFromWebhook: Creating new user from webhook. ClerkId={ClerkId}, Email={Email}", webhookData.Id, normalizedEmail ?? "(no email)");
                var newUser = await CreateUserFromWebhookAsync(webhookData);
                return MapToDto(newUser);
            }

            // Cập nhật thông tin user
            var emailUpdate = webhookData.EmailAddresses?.FirstOrDefault(e => e.IsVerified)?.EmailAddress
                ?? webhookData.EmailAddresses?.FirstOrDefault()?.EmailAddress;

            if (!string.IsNullOrEmpty(emailUpdate))
            {
                user.Email = emailUpdate;
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
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}
