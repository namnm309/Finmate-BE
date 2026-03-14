using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    [Table("tbl_community_posts")]
    public class CommunityPost : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        public int LikesCount { get; set; }

        public int CommentsCount { get; set; }

        public int SharesCount { get; set; }

        public virtual Users User { get; set; } = null!;

        public virtual ICollection<CommunityPostLike> Likes { get; set; } = new List<CommunityPostLike>();

        public virtual ICollection<CommunityPostBookmark> Bookmarks { get; set; } = new List<CommunityPostBookmark>();

        public virtual ICollection<CommunityPostComment> Comments { get; set; } = new List<CommunityPostComment>();
    }
}
