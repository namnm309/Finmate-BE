using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class CreateCategoryDto
    {
        [Required]
        public Guid TransactionTypeId { get; set; }

        public Guid? ParentCategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Icon { get; set; } = "category";

        public int DisplayOrder { get; set; } = 0;
    }
}
