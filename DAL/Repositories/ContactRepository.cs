using DAL.Data;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class ContactRepository : IContactRepository
    {
        private readonly FinmateContext _context;

        public ContactRepository(FinmateContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Contact>> GetAllByUserIdAsync(Guid userId)
        {
            return await _context.Contacts
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Contact>> GetActiveByUserIdAsync(Guid userId)
        {
            return await _context.Contacts
                .Where(c => c.UserId == userId && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Contact?> GetByIdAsync(Guid id)
        {
            return await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Contact> AddAsync(Contact contact)
        {
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();
            return contact;
        }

        public async Task<Contact> UpdateAsync(Contact contact)
        {
            _context.Contacts.Update(contact);
            await _context.SaveChangesAsync();
            return contact;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null) return false;

            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasTransactionsAsync(Guid contactId)
        {
            return await _context.Transactions
                .AnyAsync(t => t.ContactId == contactId);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
