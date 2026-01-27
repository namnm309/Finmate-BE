using DAL.Data;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class AccountTypeRepository : IAccountTypeRepository
    {
        private readonly FinmateContext _context;

        public AccountTypeRepository(FinmateContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AccountType>> GetAllAsync()
        {
            return await _context.AccountTypes
                .OrderBy(a => a.DisplayOrder)
                .ToListAsync();
        }

        public async Task<AccountType?> GetByIdAsync(Guid id)
        {
            return await _context.AccountTypes
                .FirstOrDefaultAsync(a => a.Id == id);
        }
    }
}
