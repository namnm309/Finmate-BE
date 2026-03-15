namespace BLL.DTOs.Response
{
    public class SavingsBookDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid BankId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Currency { get; set; } = "VND";
        public DateTime DepositDate { get; set; }
        public int TermMonths { get; set; }
        public decimal InterestRate { get; set; }
        public decimal NonTermInterestRate { get; set; }
        public int DaysInYearForInterest { get; set; }
        public string InterestPaymentType { get; set; } = "CuoiKy";
        public string MaturityOption { get; set; } = "TaiTucGocVaLai";
        public Guid? SourceMoneySourceId { get; set; }
        public string? Description { get; set; }
        public bool ExcludeFromReports { get; set; }
        public decimal InitialBalance { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTime MaturityDate { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
