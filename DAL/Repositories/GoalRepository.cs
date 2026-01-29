using DAL.Data;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class GoalRepository : IGoalRepository
    {
        private readonly FinmateContext _context;

        public GoalRepository(FinmateContext context)
        {
            _context = context;
        }

        public async Task<Goal?> GetByIdAsync(Guid id)
        {
            return await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<List<Goal>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Goals
                .Where(g => g.UserId == userId)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();
        }

        public async Task<Goal> AddAsync(Goal goal)
        {
            goal.CreatedAt = DateTime.UtcNow;
            goal.UpdatedAt = DateTime.UtcNow;
            _context.Goals.Add(goal);
            await _context.SaveChangesAsync();
            return goal;
        }

        public async Task<Goal> UpdateAsync(Goal goal)
        {
            goal.UpdatedAt = DateTime.UtcNow;
            _context.Goals.Update(goal);
            await _context.SaveChangesAsync();
            return goal;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var goal = await _context.Goals.FindAsync(id);
            if (goal == null)
            {
                return false;
            }

            _context.Goals.Remove(goal);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
