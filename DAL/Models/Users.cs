using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    [Table("tbl_users")]
    public class Users : BaseEntity
    {       

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        [MaxLength(255)]
        public string FullName { get; set; }

        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [MaxLength(500)]
        public string AvatarUrl { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(10)]
        public string CurrencyPreference { get; set; } = "VND";

        [MaxLength(10)]
        public string LanguagePreference { get; set; } = "vi";        

        public bool IsActive { get; set; } = true;
        public bool IsPremium { get; set; } = false;
    }
}
