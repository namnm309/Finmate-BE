namespace BLL.DTOs.Response
{
    /// <summary>
    /// DTO cho màn hình Account - group MoneySources theo AccountType
    /// </summary>
    public class MoneySourceGroupedDto
    {
        public Guid AccountTypeId { get; set; }
        public string AccountTypeName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public decimal TotalBalance { get; set; }
        public List<MoneySourceDto> MoneySources { get; set; } = new List<MoneySourceDto>();
    }

    public class MoneySourceGroupedResponseDto
    {
        public decimal TotalBalance { get; set; }
        public List<MoneySourceGroupedDto> Groups { get; set; } = new List<MoneySourceGroupedDto>();
    }
}
