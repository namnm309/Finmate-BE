namespace DAL.Models
{
    /// <summary>
    /// Enum định nghĩa các role trong hệ thống
    /// Thứ tự quyền: Admin > Staff > User
    /// </summary>
    public enum Role
    {
        User = 0,
        Staff = 1,
        Admin = 2
    }
}
