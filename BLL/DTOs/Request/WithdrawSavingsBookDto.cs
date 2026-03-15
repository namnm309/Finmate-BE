using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class WithdrawSavingsBookDto
    {
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public Guid DestinationMoneySourceId { get; set; }

        public DateTime? Date { get; set; }
    }
}
