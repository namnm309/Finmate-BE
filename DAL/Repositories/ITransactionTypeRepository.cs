using DAL.Models;

namespace DAL.Repositories
{
    public interface ITransactionTypeRepository
    {
        Task<IEnumerable<TransactionType>> GetAllAsync();
        Task<TransactionType?> GetByIdAsync(Guid id);
    }
}
