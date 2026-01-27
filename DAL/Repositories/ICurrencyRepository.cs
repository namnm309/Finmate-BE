using DAL.Models;

namespace DAL.Repositories
{
    public interface ICurrencyRepository
    {
        Task<IEnumerable<Currency>> GetAllActiveAsync();
        Task<Currency?> GetByIdAsync(Guid id);
        Task<Currency?> GetByCodeAsync(string code);
    }
}
