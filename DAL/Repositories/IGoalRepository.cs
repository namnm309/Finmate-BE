using DAL.Models;

namespace DAL.Repositories
{
    public interface IGoalRepository
    {
        Task<Goal?> GetByIdAsync(Guid id);
        Task<List<Goal>> GetByUserIdAsync(Guid userId);
        Task<Goal> AddAsync(Goal goal);
        Task<Goal> UpdateAsync(Goal goal);
        Task<bool> DeleteAsync(Guid id);
        Task<int> SaveChangesAsync();
    }
}
