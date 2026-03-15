using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class DepositSavingsBookDto
    {
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public Guid? SourceMoneySourceId { get; set; }

        public DateTime? Date { get; set; }
    }
}
