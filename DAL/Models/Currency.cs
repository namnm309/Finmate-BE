using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    /// <summary>
    /// Loại tiền tệ (System-defined, không cho user CRUD)
    /// </summary>
    [Table("tbl_currencies")]
    public class Currency
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>
        /// Mã tiền tệ ISO 4217 (VND, USD, EUR...)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Tên đầy đủ (Vietnamese Dong, United States Dollar...)
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Ký hiệu tiền tệ (₫, $, €, £...)
        /// </summary>
        [MaxLength(10)]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Mã quốc gia 2 ký tự ISO 3166-1 alpha-2 (VN, US, EU...)
        /// Dùng để hiển thị cờ quốc gia
        /// </summary>
        [MaxLength(5)]
        public string CountryCode { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
