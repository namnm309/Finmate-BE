using DAL.Data;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class MoneySourceRepository : IMoneySourceRepository
    {
        private readonly FinmateContext _context;

        public MoneySourceRepository(FinmateContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MoneySource>> GetAllByUserIdAsync(Guid userId)
        {
            return await _context.MoneySources
                .Include(m => m.AccountType)
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.AccountType.DisplayOrder)
                .ThenBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<MoneySource>> GetActiveByUserIdAsync(Guid userId)
        {
            return await _context.MoneySources
                .Include(m => m.AccountType)
                .Where(m => m.UserId == userId && m.IsActive)
                .OrderBy(m => m.AccountType.DisplayOrder)
                .ThenBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<MoneySource?> GetByIdAsync(Guid id)
        {
            return await _context.MoneySources
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<MoneySource?> GetByIdWithAccountTypeAsync(Guid id)
        {
            return await _context.MoneySources
                .Include(m => m.AccountType)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<MoneySource> AddAsync(MoneySource moneySource)
        {
            _context.MoneySources.Add(moneySource);
            await _context.SaveChangesAsync();
            return moneySource;
        }

        public async Task<MoneySource> UpdateAsync(MoneySource moneySource)
        {
            _context.MoneySources.Update(moneySource);
            await _context.SaveChangesAsync();
            return moneySource;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var moneySource = await _context.MoneySources.FindAsync(id);
            if (moneySource == null) return false;

            _context.MoneySources.Remove(moneySource);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
