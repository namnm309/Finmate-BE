namespace BLL.DTOs.Response
{
    public class MoneySourceDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid AccountTypeId { get; set; }
        public string AccountTypeName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Currency { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
