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
        [MaxLength(255)]
        public string? ClerkUserId { get; set; }

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

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(255)]
        public string? Occupation { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(10)]
        public string CurrencyPreference { get; set; } = "VND";

        [MaxLength(10)]
        public string LanguagePreference { get; set; } = "vi";        

        public bool IsActive { get; set; } = true;
        public bool IsPremium { get; set; } = false;

        // Role (User/Staff/Admin) - dùng cho basic login
        [Required]
        public Role Role { get; set; } = Role.User;

        // Navigation properties
        public virtual ICollection<MoneySource> MoneySources { get; set; } = new List<MoneySource>();
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
        public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
