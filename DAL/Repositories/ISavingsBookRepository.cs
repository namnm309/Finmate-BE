using DAL.Models;

namespace DAL.Repositories
{
    public interface ISavingsBookRepository
    {
        Task<IEnumerable<SavingsBook>> GetByUserIdAsync(Guid userId);
        Task<SavingsBook?> GetByIdAsync(Guid id);
        Task<SavingsBook?> GetByIdWithBankAsync(Guid id);
        Task<SavingsBook> AddAsync(SavingsBook savingsBook);
        Task<SavingsBook> UpdateAsync(SavingsBook savingsBook);
        Task<bool> DeleteAsync(Guid id);
        Task<int> SaveChangesAsync();
    }
}
