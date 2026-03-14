namespace BLL.DTOs.Response
{
    public class CommunityCommentDto
    {
        public Guid Id { get; set; }

        public Guid PostId { get; set; }

        public string Content { get; set; } = string.Empty;

        public Guid? ParentCommentId { get; set; }

        public DateTime CreatedAt { get; set; }

        public CommunityAuthorDto User { get; set; } = new();
    }
}
