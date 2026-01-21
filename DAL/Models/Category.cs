using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    /// <summary>
    /// Hạng mục chi tiêu (Gửi xe, Học hành, Ăn uống...)
    /// Mỗi user có bộ categories riêng, được auto-seed khi đăng ký
    /// User có thể CRUD tất cả categories của mình
    /// </summary>
    [Table("tbl_categories")]
    public class Category : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid TransactionTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Icon { get; set; } = "category";

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual Users User { get; set; } = null!;

        [ForeignKey("TransactionTypeId")]
        public virtual TransactionType TransactionType { get; set; } = null!;

        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
