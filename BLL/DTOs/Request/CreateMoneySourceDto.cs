using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class CreateMoneySourceDto
    {
        [Required]
        public Guid AccountTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Icon { get; set; } = "account-balance-wallet";

        [MaxLength(20)]
        public string Color { get; set; } = "#51A2FF";

        public decimal Balance { get; set; } = 0;

        [MaxLength(10)]
        public string Currency { get; set; } = "VND";
    }
}
