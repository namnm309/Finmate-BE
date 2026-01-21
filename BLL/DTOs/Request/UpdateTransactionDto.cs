using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class UpdateTransactionDto
    {
        public Guid? TransactionTypeId { get; set; }

        public Guid? MoneySourceId { get; set; }

        public Guid? CategoryId { get; set; }

        public Guid? ContactId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal? Amount { get; set; }

        public DateTime? TransactionDate { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool? IsBorrowingForThis { get; set; }

        public bool? IsFee { get; set; }

        public bool? ExcludeFromReport { get; set; }
    }
}
