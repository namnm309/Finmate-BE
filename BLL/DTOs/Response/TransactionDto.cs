namespace BLL.DTOs.Response
{
    public class TransactionDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        
        // Transaction Type
        public Guid TransactionTypeId { get; set; }
        public string TransactionTypeName { get; set; } = string.Empty;
        public string TransactionTypeColor { get; set; } = string.Empty;
        public bool IsIncome { get; set; }
        
        // Money Source
        public Guid MoneySourceId { get; set; }
        public string MoneySourceName { get; set; } = string.Empty;
        public string MoneySourceIcon { get; set; } = string.Empty;
        
        // Category
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryIcon { get; set; } = string.Empty;
        
        // Contact (optional)
        public Guid? ContactId { get; set; }
        public string? ContactName { get; set; }
        
        // Transaction details
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Description { get; set; }
        public bool IsBorrowingForThis { get; set; }
        public bool IsFee { get; set; }
        public bool ExcludeFromReport { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class TransactionListResponseDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
    }
}
