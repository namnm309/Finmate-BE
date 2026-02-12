using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    [Table("tbl_goals")]
    public class Goal : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TargetAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentAmount { get; set; } = 0;

        public DateTime? TargetDate { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        [MaxLength(10)]
        public string Currency { get; set; } = "VND";

        [MaxLength(50)]
        public string Icon { get; set; } = "flag";

        [MaxLength(20)]
        public string Color { get; set; } = "#51A2FF";

        public bool IsActive { get; set; } = true;

        [ForeignKey("UserId")]
        public virtual Users User { get; set; } = null!;
    }
}
