using BLL.DTOs.Request;
using BLL.DTOs.Response;
using DAL.Models;
using DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class ContactService
    {
        private readonly IContactRepository _contactRepository;
        private readonly ILogger<ContactService> _logger;

        public ContactService(IContactRepository contactRepository, ILogger<ContactService> logger)
        {
            _contactRepository = contactRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<ContactDto>> GetAllByUserIdAsync(Guid userId)
        {
            var contacts = await _contactRepository.GetAllByUserIdAsync(userId);
            return contacts.Select(MapToDto);
        }

        public async Task<IEnumerable<ContactDto>> GetActiveByUserIdAsync(Guid userId)
        {
            var contacts = await _contactRepository.GetActiveByUserIdAsync(userId);
            return contacts.Select(MapToDto);
        }

        public async Task<ContactDto?> GetByIdAsync(Guid id, Guid userId)
        {
            var contact = await _contactRepository.GetByIdAsync(id);

            if (contact == null || contact.UserId != userId)
            {
                return null;
            }

            return MapToDto(contact);
        }

        public async Task<ContactDto> CreateAsync(Guid userId, CreateContactDto request)
        {
            var contact = new Contact
            {
                UserId = userId,
                Name = request.Name,
                PhoneNumber = request.PhoneNumber,
                Note = request.Note,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _contactRepository.AddAsync(contact);

            _logger.LogInformation("Created Contact {Id} for user {UserId}", created.Id, userId);

            return MapToDto(created);
        }

        public async Task<ContactDto?> UpdateAsync(Guid id, Guid userId, UpdateContactDto request)
        {
            var contact = await _contactRepository.GetByIdAsync(id);

            if (contact == null || contact.UserId != userId)
            {
                return null;
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                contact.Name = request.Name;
            }

            if (request.PhoneNumber != null)
            {
                contact.PhoneNumber = request.PhoneNumber;
            }

            if (request.Note != null)
            {
                contact.Note = request.Note;
            }

            if (request.IsActive.HasValue)
            {
                contact.IsActive = request.IsActive.Value;
            }

            contact.UpdatedAt = DateTime.UtcNow;

            var updated = await _contactRepository.UpdateAsync(contact);

            _logger.LogInformation("Updated Contact {Id} for user {UserId}", id, userId);

            return MapToDto(updated);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(Guid id, Guid userId)
        {
            var contact = await _contactRepository.GetByIdAsync(id);

            if (contact == null || contact.UserId != userId)
            {
                return (false, "Contact not found");
            }

            // Check if contact has transactions
            if (await _contactRepository.HasTransactionsAsync(id))
            {
                // Soft delete instead of hard delete
                contact.IsActive = false;
                contact.UpdatedAt = DateTime.UtcNow;
                await _contactRepository.UpdateAsync(contact);

                _logger.LogInformation("Soft deleted Contact {Id} for user {UserId} (has transactions)", id, userId);
                return (true, null);
            }

            // Hard delete
            await _contactRepository.DeleteAsync(id);

            _logger.LogInformation("Hard deleted Contact {Id} for user {UserId}", id, userId);

            return (true, null);
        }

        private ContactDto MapToDto(Contact entity)
        {
            return new ContactDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Name = entity.Name,
                PhoneNumber = entity.PhoneNumber,
                Note = entity.Note,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
