using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BLL.DTOs.Request;
using BLL.DTOs.Response;
using DAL.Models;
using DAL.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace BLL.Services
{
    public class AuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Đăng ký tài khoản mới với email/password
        /// </summary>
        public async Task<UserResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var email = request.Email?.Trim().ToLower();
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email là bắt buộc", nameof(request));
            }

            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email đã được sử dụng.");
            }

            if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 8)
            {
                throw new ArgumentException("Mật khẩu phải có ít nhất 8 ký tự", nameof(request));
            }

            var user = new Users
            {
                ClerkUserId = null,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName?.Trim() ?? throw new ArgumentException("Họ tên là bắt buộc", nameof(request)),
                PhoneNumber = request.PhoneNumber?.Trim(),
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
        /// Đăng nhập với email/password và trả về JWT + user
        /// </summary>
        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var emailToSearch = request.Email.Trim().ToLower();
            if (emailToSearch == "admin")
            {
                emailToSearch = "admin@admin.com";
            }

            var user = await _userRepository.GetByEmailAsync(emailToSearch);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa.");
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
            }

            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            catch
            {
                isPasswordValid = false;
            }

            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

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

            var jwtSecret = _configuration["Jwt:SecretKey"] ?? "00000000000000000000000000000000";
            if (jwtSecret.Length < 32)
            {
                throw new InvalidOperationException("Jwt:SecretKey phải có ít nhất 32 ký tự");
            }

            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "FinmateAPI";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "FinmateClient";
            var expiryMinutesStr = _configuration["Jwt:ExpiryMinutes"] ?? "1440";
            if (!int.TryParse(expiryMinutesStr, out var jwtExpiryMinutes) || jwtExpiryMinutes <= 0)
            {
                jwtExpiryMinutes = 1440;
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
