namespace BLL.DTOs.Response
{
    public class AccountTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
