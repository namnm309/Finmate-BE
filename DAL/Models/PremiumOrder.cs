using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    [Table("tbl_premium_orders")]
    public class PremiumOrder : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Plan { get; set; } = string.Empty; // "1-month", "6-month", "1-year"

        [Column(TypeName = "numeric(18,0)")]
        public decimal AmountVnd { get; set; }

        /// <summary>
        /// Mã thanh toán để match từ webhook (ưu tiên SePay code, fallback parse content).
        /// Nên là chữ/số/không dấu (ví dụ: FM + base32).
        /// </summary>
        [Required]
        [MaxLength(40)]
        public string PaymentCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending|Paid|Expired|Cancelled

        [Required]
        public DateTime ExpiresAt { get; set; }

        public DateTime? PaidAt { get; set; }

        /// <summary>
        /// Sepay webhook fields
        /// </summary>
        public long? SepayTransactionId { get; set; } // payload.id

        [MaxLength(80)]
        public string? ReferenceCode { get; set; } // payload.referenceCode

        [MaxLength(100)]
        public string? BankGateway { get; set; } // payload.gateway

        [MaxLength(50)]
        public string? AccountNumber { get; set; } // payload.accountNumber

        public string? LastWebhookContent { get; set; }

        [ForeignKey("UserId")]
        public virtual Users User { get; set; } = null!;
    }
}

