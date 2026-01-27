namespace BLL.DTOs.Response
{
    public class CurrencyDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
