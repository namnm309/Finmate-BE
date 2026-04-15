using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    /// <summary>
    /// Dem luot goi AI theo user va thang (UTC, yyyy-MM).
    /// </summary>
    [Table("tbl_user_ai_monthly_usage")]
    public class UserAiMonthlyUsage : BaseEntity
    {
        public Guid UserId { get; set; }

        /// <summary>Thang UTC dang yyyy-MM.</summary>
        [Required]
        [MaxLength(7)]
        public string PeriodKey { get; set; } = string.Empty;

        public int PlanCalls { get; set; }

        public int ChatCalls { get; set; }

        public virtual Users User { get; set; } = null!;
    }
}
