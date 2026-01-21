using DAL.Models;

namespace DAL.Repositories
{
    public interface ITransactionRepository
    {
        Task<IEnumerable<Transaction>> GetAllByUserIdAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<IEnumerable<Transaction>> GetByUserIdWithFilterAsync(
            Guid userId, 
            Guid? transactionTypeId = null, 
            Guid? categoryId = null,
            Guid? moneySourceId = null,
            DateTime? startDate = null, 
            DateTime? endDate = null,
            int page = 1, 
            int pageSize = 20);
        Task<Transaction?> GetByIdAsync(Guid id);
        Task<Transaction?> GetByIdWithDetailsAsync(Guid id);
        Task<Transaction> AddAsync(Transaction transaction);
        Task<Transaction> UpdateAsync(Transaction transaction);
        Task<bool> DeleteAsync(Guid id);
        Task<int> CountByUserIdAsync(Guid userId);
        Task<int> SaveChangesAsync();
    }
}
