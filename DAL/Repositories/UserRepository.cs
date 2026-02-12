using DAL.Data;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly FinmateContext _context;

        public UserRepository(FinmateContext context)
        {
            _context = context;
        }

        public async Task<Users?> GetByClerkUserIdAsync(string clerkUserId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.ClerkUserId == clerkUserId);
        }

        public async Task<Users?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<Users?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var normalized = email.Trim().ToLower();
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.Trim().ToLower() == normalized);
        }

        public async Task<Users?> GetByEmailNormalizedAsync(string normalizedEmail)
        {
            if (string.IsNullOrWhiteSpace(normalizedEmail)) return null;
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.Trim().ToLower() == normalizedEmail);
        }

        public async Task<Users> AddAsync(Users user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<Users> UpdateAsync(Users user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
