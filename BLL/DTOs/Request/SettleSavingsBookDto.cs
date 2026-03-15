using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class SettleSavingsBookDto
    {
        /// <summary>
        /// Số tiền tất toán. Nếu null hoặc 0 thì dùng toàn bộ CurrentBalance.
        /// </summary>
        public decimal? Amount { get; set; }

        [Required]
        public DateTime SettlementDate { get; set; }

        [Required]
        public Guid DestinationMoneySourceId { get; set; }
    }
}
