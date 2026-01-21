using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    /// <summary>
    /// Loại tài khoản (System-defined, không cho user CRUD)
    /// 6 loại cố định: Tiền mặt, Tài khoản ngân hàng, Thẻ tín dụng, Tài khoản đầu tư, Ví điện tử, Khác
    /// </summary>
    [Table("tbl_account_types")]
    public class AccountType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation property
        public virtual ICollection<MoneySource> MoneySources { get; set; } = new List<MoneySource>();
    }
}
