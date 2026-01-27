using DAL.Models;

namespace DAL.Repositories
{
    public interface IMoneySourceRepository
    {
        Task<IEnumerable<MoneySource>> GetAllByUserIdAsync(Guid userId);
        Task<IEnumerable<MoneySource>> GetActiveByUserIdAsync(Guid userId);
        Task<MoneySource?> GetByIdAsync(Guid id);
        Task<MoneySource?> GetByIdWithAccountTypeAsync(Guid id);
        Task<MoneySource> AddAsync(MoneySource moneySource);
        Task<MoneySource> UpdateAsync(MoneySource moneySource);
        Task<bool> DeleteAsync(Guid id);
        Task<int> SaveChangesAsync();
    }
}
