using BLL.DTOs.Response;
using DAL.Models;
using DAL.Repositories;

namespace BLL.Services
{
    public class CurrencyService
    {
        private readonly ICurrencyRepository _currencyRepository;

        public CurrencyService(ICurrencyRepository currencyRepository)
        {
            _currencyRepository = currencyRepository;
        }

        public async Task<IEnumerable<CurrencyDto>> GetAllAsync()
        {
            var currencies = await _currencyRepository.GetAllActiveAsync();
            return currencies.Select(MapToDto);
        }

        public async Task<CurrencyDto?> GetByIdAsync(Guid id)
        {
            var currency = await _currencyRepository.GetByIdAsync(id);
            return currency != null ? MapToDto(currency) : null;
        }

        public async Task<CurrencyDto?> GetByCodeAsync(string code)
        {
            var currency = await _currencyRepository.GetByCodeAsync(code);
            return currency != null ? MapToDto(currency) : null;
        }

        private CurrencyDto MapToDto(Currency entity)
        {
            return new CurrencyDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Symbol = entity.Symbol,
                CountryCode = entity.CountryCode,
                DisplayOrder = entity.DisplayOrder
            };
        }
    }
}
