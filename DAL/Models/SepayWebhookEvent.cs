using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    [Table("tbl_sepay_webhook_events")]
    public class SepayWebhookEvent : BaseEntity
    {
        /// <summary>
        /// ID giao dịch trên SePay (payload.id). Dùng làm idempotency key chính.
        /// </summary>
        [Required]
        public long SepayId { get; set; }

        [MaxLength(40)]
        public string? Code { get; set; } // payload.code

        public string? Content { get; set; } // payload.content

        [Required]
        [MaxLength(10)]
        public string TransferType { get; set; } = "in"; // in|out

        [Column(TypeName = "numeric(18,0)")]
        public decimal TransferAmount { get; set; }

        [MaxLength(80)]
        public string? ReferenceCode { get; set; }

        public DateTime? TransactionDate { get; set; }

        public string? RawPayload { get; set; }
    }
}

