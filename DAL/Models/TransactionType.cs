using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    /// <summary>
    /// Loại giao dịch (System-defined, không cho user CRUD)
    /// 4 loại cố định: Chi tiêu, Thu tiền, Cho vay, Đi vay
    /// </summary>
    [Table("tbl_transaction_types")]
    public class TransactionType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Color { get; set; } = "#000000";

        /// <summary>
        /// true = thu tiền (Thu tiền, Đi vay) -> Balance tăng
        /// false = chi tiền (Chi tiêu, Cho vay) -> Balance giảm
        /// </summary>
        public bool IsIncome { get; set; }

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
