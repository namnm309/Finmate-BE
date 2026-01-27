using DAL.Data;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly FinmateContext _context;

        public TransactionRepository(FinmateContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Transaction>> GetAllByUserIdAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            return await _context.Transactions
                .Include(t => t.TransactionType)
                .Include(t => t.MoneySource)
                .Include(t => t.Category)
                .Include(t => t.Contact)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetByUserIdWithFilterAsync(
            Guid userId,
            Guid? transactionTypeId = null,
            Guid? categoryId = null,
            Guid? moneySourceId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.Transactions
                .Include(t => t.TransactionType)
                .Include(t => t.MoneySource)
                .Include(t => t.Category)
                .Include(t => t.Contact)
                .Where(t => t.UserId == userId);

            if (transactionTypeId.HasValue)
            {
                query = query.Where(t => t.TransactionTypeId == transactionTypeId.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == categoryId.Value);
            }

            if (moneySourceId.HasValue)
            {
                query = query.Where(t => t.MoneySourceId == moneySourceId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate <= endDate.Value);
            }

            return await query
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Transaction?> GetByIdAsync(Guid id)
        {
            return await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Transaction?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.Transactions
                .Include(t => t.TransactionType)
                .Include(t => t.MoneySource)
                .Include(t => t.Category)
                .Include(t => t.Contact)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Transaction> AddAsync(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<Transaction> UpdateAsync(Transaction transaction)
        {
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return false;

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CountByUserIdAsync(Guid userId)
        {
            return await _context.Transactions
                .CountAsync(t => t.UserId == userId);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
