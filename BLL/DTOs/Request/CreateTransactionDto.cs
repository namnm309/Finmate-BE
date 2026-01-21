using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class CreateTransactionDto
    {
        [Required]
        public Guid TransactionTypeId { get; set; }

        [Required]
        public Guid MoneySourceId { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        /// <summary>
        /// Người liên quan (optional) - dùng cho Cho vay, Đi vay
        /// </summary>
        public Guid? ContactId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsBorrowingForThis { get; set; } = false;

        public bool IsFee { get; set; } = false;

        public bool ExcludeFromReport { get; set; } = false;
    }
}
