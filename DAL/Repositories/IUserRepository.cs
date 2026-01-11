using DAL.Models;

namespace DAL.Repositories
{
    public interface IUserRepository
    {
        Task<Users?> GetByClerkUserIdAsync(string clerkUserId);
        Task<Users?> GetByIdAsync(Guid id);
        Task<Users> AddAsync(Users user);
        Task<Users> UpdateAsync(Users user);
        Task<int> SaveChangesAsync();
    }
}
