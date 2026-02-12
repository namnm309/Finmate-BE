using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class UpdateCategoryDto
    {
        public Guid? TransactionTypeId { get; set; }

        public Guid? ParentCategoryId { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(50)]
        public string? Icon { get; set; }

        public int? DisplayOrder { get; set; }

        public bool? IsActive { get; set; }
    }
}
