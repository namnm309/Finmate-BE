using BLL.DTOs.Response;
using DAL.Models;
using DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class TransactionTypeService
    {
        private readonly ITransactionTypeRepository _transactionTypeRepository;
        private readonly ILogger<TransactionTypeService> _logger;

        public TransactionTypeService(ITransactionTypeRepository transactionTypeRepository, ILogger<TransactionTypeService> logger)
        {
            _transactionTypeRepository = transactionTypeRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<TransactionTypeDto>> GetAllAsync()
        {
            var transactionTypes = await _transactionTypeRepository.GetAllAsync();
            return transactionTypes.Select(MapToDto);
        }

        public async Task<TransactionTypeDto?> GetByIdAsync(Guid id)
        {
            var transactionType = await _transactionTypeRepository.GetByIdAsync(id);
            return transactionType != null ? MapToDto(transactionType) : null;
        }

        private TransactionTypeDto MapToDto(TransactionType entity)
        {
            return new TransactionTypeDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Color = entity.Color,
                IsIncome = entity.IsIncome,
                DisplayOrder = entity.DisplayOrder
            };
        }
    }
}
