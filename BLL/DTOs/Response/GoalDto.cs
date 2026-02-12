using System;

namespace BLL.DTOs.Response
{
    public class GoalDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public DateTime? TargetDate { get; set; }
        public string Status { get; set; } = "Active";
        public string Currency { get; set; } = "VND";
        public string Icon { get; set; } = "flag";
        public string Color { get; set; } = "#51A2FF";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal ProgressPercentage { get; set; }
    }
}
