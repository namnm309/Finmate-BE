using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    /// <summary>
    /// Sổ tiết kiệm của user
    /// </summary>
    [Table("tbl_savings_books")]
    public class SavingsBook : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid BankId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Currency { get; set; } = "VND";

        [Required]
        public DateTime DepositDate { get; set; }

        [Required]
        public int TermMonths { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal InterestRate { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal NonTermInterestRate { get; set; } = 0;

        public int DaysInYearForInterest { get; set; } = 365;

        [MaxLength(20)]
        public string InterestPaymentType { get; set; } = "CuoiKy"; // CuoiKy | DauKy

        [MaxLength(30)]
        public string MaturityOption { get; set; } = "TaiTucGocVaLai"; // TaiTucGocVaLai | TaiTucGoc | TatToanSo

        public Guid? SourceMoneySourceId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool ExcludeFromReports { get; set; } = false;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal InitialBalance { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentBalance { get; set; } = 0;

        [Required]
        public DateTime MaturityDate { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active | Closed

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual Users User { get; set; } = null!;

        [ForeignKey("BankId")]
        public virtual Bank Bank { get; set; } = null!;

        [ForeignKey("SourceMoneySourceId")]
        public virtual MoneySource? SourceMoneySource { get; set; }
    }
}
