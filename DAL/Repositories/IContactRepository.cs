using DAL.Models;

namespace DAL.Repositories
{
    public interface IContactRepository
    {
        Task<IEnumerable<Contact>> GetAllByUserIdAsync(Guid userId);
        Task<IEnumerable<Contact>> GetActiveByUserIdAsync(Guid userId);
        Task<Contact?> GetByIdAsync(Guid id);
        Task<Contact> AddAsync(Contact contact);
        Task<Contact> UpdateAsync(Contact contact);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> HasTransactionsAsync(Guid contactId);
        Task<int> SaveChangesAsync();
    }
}
