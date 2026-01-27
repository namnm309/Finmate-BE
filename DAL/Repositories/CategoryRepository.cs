using DAL.Data;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly FinmateContext _context;

        public CategoryRepository(FinmateContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetAllByUserIdAsync(Guid userId)
        {
            return await _context.Categories
                .Include(c => c.TransactionType)
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Category>> GetActiveByUserIdAsync(Guid userId)
        {
            return await _context.Categories
                .Include(c => c.TransactionType)
                .Where(c => c.UserId == userId && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Category>> GetByUserIdAndTransactionTypeAsync(Guid userId, Guid transactionTypeId)
        {
            return await _context.Categories
                .Include(c => c.TransactionType)
                .Where(c => c.UserId == userId && c.TransactionTypeId == transactionTypeId && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(Guid id)
        {
            return await _context.Categories
                .Include(c => c.TransactionType)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> ExistsByNameForUserAsync(Guid userId, string name, Guid? excludeId = null)
        {
            var query = _context.Categories
                .Where(c => c.UserId == userId && c.Name.ToLower() == name.ToLower());

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<Category> AddAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task AddRangeAsync(IEnumerable<Category> categories)
        {
            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();
        }

        public async Task<Category> UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasTransactionsAsync(Guid categoryId)
        {
            return await _context.Transactions
                .AnyAsync(t => t.CategoryId == categoryId);
        }

        public async Task<int> CountByUserIdAsync(Guid userId)
        {
            return await _context.Categories
                .CountAsync(c => c.UserId == userId);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
