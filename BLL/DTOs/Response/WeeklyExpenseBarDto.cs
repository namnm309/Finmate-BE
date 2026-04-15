namespace BLL.DTOs.Response
{
    /// <summary>
    /// Mot cot trong bieu do so sanh chi tieu tuan nay / tuan truoc (theo UTC, bat dau Chu nhat).
    /// </summary>
    public class WeeklyExpenseBarDto
    {
        public string Day { get; set; } = string.Empty;
        public double ThisWeek { get; set; }
        public double LastWeek { get; set; }
    }
}
