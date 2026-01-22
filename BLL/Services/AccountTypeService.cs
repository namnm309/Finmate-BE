using BLL.DTOs.Response;
using DAL.Models;
using DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class AccountTypeService
    {
        private readonly IAccountTypeRepository _accountTypeRepository;
        private readonly ILogger<AccountTypeService> _logger;

        public AccountTypeService(IAccountTypeRepository accountTypeRepository, ILogger<AccountTypeService> logger)
        {
            _accountTypeRepository = accountTypeRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<AccountTypeDto>> GetAllAsync()
        {
            var accountTypes = await _accountTypeRepository.GetAllAsync();
            return accountTypes.Select(MapToDto);
        }

        public async Task<AccountTypeDto?> GetByIdAsync(Guid id)
        {
            var accountType = await _accountTypeRepository.GetByIdAsync(id);
            return accountType != null ? MapToDto(accountType) : null;
        }

        private AccountTypeDto MapToDto(AccountType entity)
        {
            return new AccountTypeDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Icon = entity.Icon,
                Color = entity.Color,
                DisplayOrder = entity.DisplayOrder
            };
        }
    }
}
