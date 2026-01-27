namespace BLL.DTOs.Response
{
    /// <summary>
    /// DTO cho báo cáo tổng quan thu/chi
    /// </summary>
    public class OverviewReportDto
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal Difference { get; set; }
        public List<CategoryStatDto> CategoryStats { get; set; } = new List<CategoryStatDto>();
    }
}
