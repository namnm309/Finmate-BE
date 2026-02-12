using DAL.Models;

namespace BLL.DTOs.Response
{
    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string? ClerkUserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Address { get; set; }
        public string? Occupation { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool IsActive { get; set; }
        public bool IsPremium { get; set; }
        public Role Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        //
    }
}
