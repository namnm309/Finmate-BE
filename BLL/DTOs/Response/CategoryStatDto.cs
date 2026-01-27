namespace BLL.DTOs.Response
{
    /// <summary>
    /// Thống kê theo danh mục trong báo cáo tổng quan
    /// </summary>
    public class CategoryStatDto
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryIcon { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; } = "#000000";
    }
}
