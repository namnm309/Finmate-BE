namespace BLL.DTOs.Response
{
    public class CommunityPostDto
    {
        public Guid Id { get; set; }

        public string Category { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public int LikesCount { get; set; }

        public int CommentsCount { get; set; }

        public int SharesCount { get; set; }

        public bool LikedByCurrentUser { get; set; }

        public bool BookmarkedByCurrentUser { get; set; }

        public bool IsFollowingByCurrentUser { get; set; }

        public bool IsOwnPost { get; set; }

        public DateTime CreatedAt { get; set; }

        public CommunityAuthorDto User { get; set; } = new();
    }
}
