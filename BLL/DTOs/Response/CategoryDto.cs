namespace BLL.DTOs.Response
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid TransactionTypeId { get; set; }
        public string TransactionTypeName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
