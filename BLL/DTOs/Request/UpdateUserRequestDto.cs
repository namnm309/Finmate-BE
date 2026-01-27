namespace BLL.DTOs.Request
{
    public class UpdateUserRequestDto
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Occupation { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
