namespace DAL.Models
{
    /// <summary>
    /// Enum định nghĩa các role trong hệ thống
    /// Thứ tự quyền: Admin > Staff > User
    /// </summary>
    public enum Role
    {
        /// <summary>
        /// User thường - quyền thấp nhất
        /// </summary>
        User = 0,

        /// <summary>
        /// Staff - quyền trung bình
        /// </summary>
        Staff = 1,

        /// <summary>
        /// Admin - quyền cao nhất
        /// </summary>
        Admin = 2
    }
}
