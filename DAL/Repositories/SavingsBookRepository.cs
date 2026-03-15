using DAL.Data;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class SavingsBookRepository : ISavingsBookRepository
    {
        private readonly FinmateContext _context;

        public SavingsBookRepository(FinmateContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SavingsBook>> GetByUserIdAsync(Guid userId)
        {
            return await _context.SavingsBooks
                .Include(s => s.Bank)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<SavingsBook?> GetByIdAsync(Guid id)
        {
            return await _context.SavingsBooks.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<SavingsBook?> GetByIdWithBankAsync(Guid id)
        {
            return await _context.SavingsBooks
                .Include(s => s.Bank)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<SavingsBook> AddAsync(SavingsBook savingsBook)
        {
            _context.SavingsBooks.Add(savingsBook);
            await _context.SaveChangesAsync();
            return savingsBook;
        }

        public async Task<SavingsBook> UpdateAsync(SavingsBook savingsBook)
        {
            _context.SavingsBooks.Update(savingsBook);
            await _context.SaveChangesAsync();
            return savingsBook;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.SavingsBooks.FindAsync(id);
            if (entity == null) return false;
            _context.SavingsBooks.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
