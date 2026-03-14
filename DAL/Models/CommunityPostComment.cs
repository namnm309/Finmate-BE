using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    [Table("tbl_community_post_comments")]
    public class CommunityPostComment : BaseEntity
    {
        [Required]
        public Guid PostId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public Guid? ParentCommentId { get; set; }

        public virtual CommunityPost Post { get; set; } = null!;

        public virtual Users User { get; set; } = null!;

        public virtual CommunityPostComment? ParentComment { get; set; }

        public virtual ICollection<CommunityPostComment> Replies { get; set; } = new List<CommunityPostComment>();
    }
}
