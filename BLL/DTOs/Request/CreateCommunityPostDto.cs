using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class CreateCommunityPostDto
    {
        [Required(ErrorMessage = "Category is required")]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required")]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }
}
