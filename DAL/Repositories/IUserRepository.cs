using DAL.Models;

namespace DAL.Repositories
{
    public interface IUserRepository
    {
        Task<Users?> GetByClerkUserIdAsync(string clerkUserId);
        Task<Users?> GetByIdAsync(Guid id);
        Task<Users?> GetByEmailAsync(string email);
        /// <summary>Tra cứu theo email đã chuẩn hóa (không phân biệt hoa thường).</summary>
        Task<Users?> GetByEmailNormalizedAsync(string normalizedEmail);
        Task<Users> AddAsync(Users user);
        Task<Users> UpdateAsync(Users user);
        Task<int> SaveChangesAsync();
    }
}
