using DAL.Models;

namespace DAL.Repositories
{
    public interface IAccountTypeRepository
    {
        Task<IEnumerable<AccountType>> GetAllAsync();
        Task<AccountType?> GetByIdAsync(Guid id);
    }
}
