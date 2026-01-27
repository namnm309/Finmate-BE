using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    /// <summary>
    /// Nguồn tiền của user (Ví dụ: Ví 1, Vietcombank, Momo...)
    /// User có thể CRUD
    /// </summary>
    [Table("tbl_money_sources")]
    public class MoneySource : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid AccountTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Icon { get; set; } = "account-balance-wallet";

        [MaxLength(20)]
        public string Color { get; set; } = "#51A2FF";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0;

        [MaxLength(10)]
        public string Currency { get; set; } = "VND";

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual Users User { get; set; } = null!;

        [ForeignKey("AccountTypeId")]
        public virtual AccountType AccountType { get; set; } = null!;

        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
