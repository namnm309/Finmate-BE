using DAL.Data;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class BankRepository : IBankRepository
    {
        private readonly FinmateContext _context;

        public BankRepository(FinmateContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Bank>> GetAllAsync()
        {
            return await _context.Banks
                .Where(b => b.IsActive)
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();
        }

        public async Task<Bank?> GetByIdAsync(Guid id)
        {
            return await _context.Banks.FirstOrDefaultAsync(b => b.Id == id);
        }
    }
}
