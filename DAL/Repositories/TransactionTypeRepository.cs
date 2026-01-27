using DAL.Data;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class TransactionTypeRepository : ITransactionTypeRepository
    {
        private readonly FinmateContext _context;

        public TransactionTypeRepository(FinmateContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TransactionType>> GetAllAsync()
        {
            return await _context.TransactionTypes
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();
        }

        public async Task<TransactionType?> GetByIdAsync(Guid id)
        {
            return await _context.TransactionTypes
                .FirstOrDefaultAsync(t => t.Id == id);
        }
    }
}
