using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class CreateCommunityCommentDto
    {
        [Required(ErrorMessage = "Content is required")]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public Guid? ParentCommentId { get; set; }
    }
}
