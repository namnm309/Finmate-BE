using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    [Table("tbl_premium_subscriptions")]
    public class PremiumSubscription : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Plan { get; set; } = string.Empty; // "1-month", "6-month", "1-year"

        [Required]
        public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [MaxLength(255)]
        public string? TransactionId { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public virtual Users User { get; set; } = null!;
    }
}
