using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class UpdateContactDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public bool? IsActive { get; set; }
    }
}
