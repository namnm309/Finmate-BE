using DAL.Models;

namespace DAL.Repositories
{
    public interface IBankRepository
    {
        Task<IEnumerable<Bank>> GetAllAsync();
        Task<Bank?> GetByIdAsync(Guid id);
    }
}
