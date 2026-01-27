using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Request
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email or username is required")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }
}
