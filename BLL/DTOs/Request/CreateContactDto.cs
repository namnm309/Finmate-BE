using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class CreateContactDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
    }
}
