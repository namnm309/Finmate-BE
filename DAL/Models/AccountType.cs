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

        /// <summary>
        /// Icon MaterialIcons name (account-balance-wallet, account-balance, credit-card...)
        /// </summary>
        [MaxLength(50)]
        public string Icon { get; set; } = "account-balance-wallet";

        /// <summary>
        /// Màu hiển thị (hex color)
        /// </summary>
        [MaxLength(20)]
        public string Color { get; set; } = "#51A2FF";

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation property
        public virtual ICollection<MoneySource> MoneySources { get; set; } = new List<MoneySource>();
    }
}
