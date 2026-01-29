using System;
using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class UpdateGoalDto
    {
        [MaxLength(255)]
        public string? Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Target amount must be greater than 0")]
        public decimal? TargetAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Current amount cannot be negative")]
        public decimal? CurrentAmount { get; set; }

        public DateTime? TargetDate { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        [MaxLength(50)]
        public string? Icon { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; }

        public bool? IsActive { get; set; }
    }
}
