using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BLL.DTOs.Request;
using BLL.DTOs.Response;
using DAL.Models;
using DAL.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BLL.Services
{
    public class AuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        /// <summary>
        /// Đăng ký user mới
        /// </summary>
        public async Task<UserResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Validate và normalize email
            var email = request.Email?.Trim().ToLower();
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email is required", nameof(request));
            }

            // Kiểm tra email đã tồn tại chưa
            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email already exists");
            }

            // Validate password
            if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters long", nameof(request));
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Tạo user mới với role mặc định là User
            var user = new Users
            {
                Email = email,
                PasswordHash = passwordHash,
                FullName = request.FullName?.Trim() ?? throw new ArgumentException("FullName is required", nameof(request)),
                PhoneNumber = request.PhoneNumber?.Trim(),
                // DB đang set AvatarUrl NOT NULL, nên phải gán default để tránh lỗi 23502
                AvatarUrl = "",
                IsActive = true,
                IsPremium = false,
                Role = Role.User,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.AddAsync(user);

            return MapToDto(createdUser);
        }

        /// <summary>
        /// Đăng nhập và trả về JWT token
        /// </summary>
        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // Xử lý đặc biệt cho admin: cho phép login bằng "admin" hoặc "admin@admin.com"
            var emailToSearch = request.Email.ToLower();
            if (emailToSearch == "admin")
            {
                emailToSearch = "admin@admin.com";
            }

            // Tìm user theo email
            var user = await _userRepository.GetByEmailAsync(emailToSearch);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Kiểm tra user có active không
            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("User account is deactivated");
            }

            // Kiểm tra password
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Verify password với try-catch để handle invalid hash format
            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            catch
            {
                // Nếu hash format không đúng, coi như password không hợp lệ
                isPasswordValid = false;
            }

            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Cập nhật LastLoginAt
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            // Tạo JWT token
            var token = GenerateJwtToken(user);

            return new LoginResponseDto
            {
                Token = token,
                User = MapToDto(user)
            };
        }

        /// <summary>
        /// Tạo JWT token
        /// </summary>
        private string GenerateJwtToken(Users user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var jwtSecret = _configuration["Jwt:SecretKey"] 
                ?? throw new InvalidOperationException("Jwt:SecretKey is not configured");
            
            // Validate JWT SecretKey length
            if (jwtSecret.Length < 32)
            {
                throw new InvalidOperationException("Jwt:SecretKey must be at least 32 characters long for security");
            }
            
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "FinmateAPI";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "FinmateClient";
            
            // Parse expiry minutes với validation
            var expiryMinutesStr = _configuration["Jwt:ExpiryMinutes"] ?? "1440";
            if (!int.TryParse(expiryMinutesStr, out var jwtExpiryMinutes) || jwtExpiryMinutes <= 0)
            {
                jwtExpiryMinutes = 1440; // Default 24 hours nếu parse fail
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(jwtExpiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
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
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}
