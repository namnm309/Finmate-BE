using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    [Table("tbl_community_post_likes")]
    public class CommunityPostLike : BaseEntity
    {
        [Required]
        public Guid PostId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public virtual CommunityPost Post { get; set; } = null!;

        public virtual Users User { get; set; } = null!;
    }
}
