using System;
using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class CreateGoalDto
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Target amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Target amount must be greater than 0")]
        public decimal TargetAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Current amount cannot be negative")]
        public decimal CurrentAmount { get; set; } = 0;

        public DateTime? TargetDate { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "VND";

        [MaxLength(50)]
        public string Icon { get; set; } = "flag";

        [MaxLength(20)]
        public string Color { get; set; } = "#51A2FF";
    }
}
