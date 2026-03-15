using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class UpdateSavingsBookDto
    {
        [MaxLength(255)]
        public string? Name { get; set; }

        public Guid? BankId { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        public DateTime? DepositDate { get; set; }

        [Range(1, int.MaxValue)]
        public int? TermMonths { get; set; }

        public decimal? InterestRate { get; set; }

        public decimal? NonTermInterestRate { get; set; }

        [Range(1, 366)]
        public int? DaysInYearForInterest { get; set; }

        [MaxLength(20)]
        public string? InterestPaymentType { get; set; }

        [MaxLength(30)]
        public string? MaturityOption { get; set; }

        public Guid? SourceMoneySourceId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool? ExcludeFromReports { get; set; }
    }
}
