using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    /// <summary>
    /// Giao dịch tài chính
    /// Khi tạo/sửa/xóa sẽ tự động cập nhật Balance của MoneySource
    /// </summary>
    [Table("tbl_transactions")]
    public class Transaction : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid TransactionTypeId { get; set; }

        [Required]
        public Guid MoneySourceId { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        /// <summary>
        /// Người liên quan (optional) - dùng cho Cho vay, Đi vay
        /// </summary>
        public Guid? ContactId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Đi vay để trả khoản này
        /// </summary>
        public bool IsBorrowingForThis { get; set; } = false;

        /// <summary>
        /// Đánh dấu là phí
        /// </summary>
        public bool IsFee { get; set; } = false;

        /// <summary>
        /// Không tính vào báo cáo
        /// </summary>
        public bool ExcludeFromReport { get; set; } = false;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual Users User { get; set; } = null!;

        [ForeignKey("TransactionTypeId")]
        public virtual TransactionType TransactionType { get; set; } = null!;

        [ForeignKey("MoneySourceId")]
        public virtual MoneySource MoneySource { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;

        [ForeignKey("ContactId")]
        public virtual Contact? Contact { get; set; }
    }
}
