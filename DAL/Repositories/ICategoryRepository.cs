using DAL.Models;

namespace DAL.Repositories
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetAllByUserIdAsync(Guid userId);
        Task<IEnumerable<Category>> GetActiveByUserIdAsync(Guid userId);
        Task<IEnumerable<Category>> GetByUserIdAndTransactionTypeAsync(Guid userId, Guid transactionTypeId);
        Task<Category?> GetByIdAsync(Guid id);
        Task<bool> ExistsByNameForUserAsync(Guid userId, Guid transactionTypeId, string name, Guid? excludeId = null);
        Task<bool> HasChildrenAsync(Guid categoryId);
        Task<IEnumerable<Category>> GetChildrenAsync(Guid categoryId);
        Task<Category> AddAsync(Category category);
        Task AddRangeAsync(IEnumerable<Category> categories);
        Task<Category> UpdateAsync(Category category);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> HasTransactionsAsync(Guid categoryId);
        Task<int> CountByUserIdAsync(Guid userId);
        Task<int> SaveChangesAsync();
    }
}
