using BLL.DTOs.Response;
using DAL.Models;
using DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class BankService
    {
        private readonly IBankRepository _bankRepository;
        private readonly ILogger<BankService> _logger;

        public BankService(IBankRepository bankRepository, ILogger<BankService> logger)
        {
            _bankRepository = bankRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<BankDto>> GetAllAsync()
        {
            var banks = await _bankRepository.GetAllAsync();
            return banks.Select(MapToDto);
        }

        public async Task<BankDto?> GetByIdAsync(Guid id)
        {
            var bank = await _bankRepository.GetByIdAsync(id);
            return bank != null ? MapToDto(bank) : null;
        }

        private static BankDto MapToDto(Bank entity)
        {
            return new BankDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Code = entity.Code,
                DisplayOrder = entity.DisplayOrder
            };
        }
    }
}
