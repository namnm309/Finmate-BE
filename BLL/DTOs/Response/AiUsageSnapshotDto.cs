namespace BLL.DTOs.Response
{
    /// <summary>Thong ke luot dung AI trong thang UTC hien tai.</summary>
    public class AiUsageSnapshotDto
    {
        public string PeriodKey { get; set; } = string.Empty;

        public int PlanCallsUsed { get; set; }
        public int PlanCallsLimit { get; set; }

        public int ChatCallsUsed { get; set; }
        public int ChatCallsLimit { get; set; }

        public bool IsPremium { get; set; }
    }
}
