using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class UpdateMoneySourceDto
    {
        public Guid? AccountTypeId { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(50)]
        public string? Icon { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; }

        public decimal? Balance { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        public bool? IsActive { get; set; }
    }
}
