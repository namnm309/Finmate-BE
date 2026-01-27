using DAL.Data;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class CurrencyRepository : ICurrencyRepository
    {
        private readonly FinmateContext _context;

        public CurrencyRepository(FinmateContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Currency>> GetAllActiveAsync()
        {
            return await _context.Currencies
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        }

        public async Task<Currency?> GetByIdAsync(Guid id)
        {
            return await _context.Currencies.FindAsync(id);
        }

        public async Task<Currency?> GetByCodeAsync(string code)
        {
            return await _context.Currencies
                .FirstOrDefaultAsync(c => c.Code == code && c.IsActive);
        }
    }
}
