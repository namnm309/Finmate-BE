using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class CreateSavingsBookDto
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Guid BankId { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "VND";

        [Required]
        public DateTime DepositDate { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int TermMonths { get; set; }

        public decimal InterestRate { get; set; } = 0;

        public decimal NonTermInterestRate { get; set; } = 0;

        [Range(1, 366)]
        public int DaysInYearForInterest { get; set; } = 365;

        [MaxLength(20)]
        public string InterestPaymentType { get; set; } = "CuoiKy";

        [MaxLength(30)]
        public string MaturityOption { get; set; } = "TaiTucGocVaLai";

        public Guid? SourceMoneySourceId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool ExcludeFromReports { get; set; } = false;

        public decimal InitialBalance { get; set; } = 0;
    }
}
