using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    [Table("tbl_premium_plan_configs")]
    public class PremiumPlanConfig
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Plan { get; set; } = string.Empty; // '1-month' | '6-month' | '1-year'

        [Column(TypeName = "numeric(18,0)")]
        public decimal PriceVnd { get; set; }

        [Column(TypeName = "numeric(18,0)")]
        public decimal? OriginalPriceVnd { get; set; }

        public int? DiscountPercent { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }
    }
}

