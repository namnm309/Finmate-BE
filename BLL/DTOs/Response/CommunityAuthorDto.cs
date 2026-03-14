namespace BLL.DTOs.Response
{
    public class CommunityAuthorDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Initials { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public bool Verified { get; set; }
    }
}
