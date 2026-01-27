namespace BLL.DTOs.Response
{
    public class TransactionTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public bool IsIncome { get; set; }
        public int DisplayOrder { get; set; }
    }
}
