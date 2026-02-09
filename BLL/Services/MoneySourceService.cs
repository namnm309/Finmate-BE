using BLL.DTOs.Request;
using BLL.DTOs.Response;
using DAL.Models;
using DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class MoneySourceService
    {
        private readonly IMoneySourceRepository _moneySourceRepository;
        private readonly IAccountTypeRepository _accountTypeRepository;
        private readonly ILogger<MoneySourceService> _logger;

        public MoneySourceService(
            IMoneySourceRepository moneySourceRepository,
            IAccountTypeRepository accountTypeRepository,
            ILogger<MoneySourceService> logger)
        {
            _moneySourceRepository = moneySourceRepository;
            _accountTypeRepository = accountTypeRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<MoneySourceDto>> GetAllByUserIdAsync(Guid userId)
        {
            var moneySources = await _moneySourceRepository.GetAllByUserIdAsync(userId);
            return moneySources.Select(MapToDto);
        }

        public async Task<IEnumerable<MoneySourceDto>> GetActiveByUserIdAsync(Guid userId)
        {
            var moneySources = await _moneySourceRepository.GetActiveByUserIdAsync(userId);
            return moneySources.Select(MapToDto);
        }

        /// <summary>
        /// Lấy danh sách nguồn tiền đã group theo AccountType (cho màn hình Account)
        /// </summary>
        public async Task<MoneySourceGroupedResponseDto> GetGroupedByUserIdAsync(Guid userId)
        {
            var moneySources = await _moneySourceRepository.GetActiveByUserIdAsync(userId);
            var accountTypes = await _accountTypeRepository.GetAllAsync();

            var groups = accountTypes
                .Select(at => new MoneySourceGroupedDto
                {
                    AccountTypeId = at.Id,
                    AccountTypeName = at.Name,
                    DisplayOrder = at.DisplayOrder,
                    MoneySources = moneySources
                        .Where(ms => ms.AccountTypeId == at.Id)
                        .Select(MapToDto)
                        .ToList(),
                    TotalBalance = moneySources
                        .Where(ms => ms.AccountTypeId == at.Id)
                        .Sum(ms => ms.Balance)
                })
                .Where(g => g.MoneySources.Any()) // Chỉ hiển thị nhóm có dữ liệu
                .OrderBy(g => g.DisplayOrder)
                .ToList();

            return new MoneySourceGroupedResponseDto
            {
                TotalBalance = moneySources.Sum(ms => ms.Balance),
                Groups = groups
            };
        }

        public async Task<MoneySourceDto?> GetByIdAsync(Guid id, Guid userId)
        {
            var moneySource = await _moneySourceRepository.GetByIdWithAccountTypeAsync(id);
            
            if (moneySource == null || moneySource.UserId != userId)
            {
                return null;
            }

            return MapToDto(moneySource);
        }

        public async Task<MoneySourceDto> CreateAsync(Guid userId, CreateMoneySourceDto request)
        {
            // Validate AccountType exists
            var accountType = await _accountTypeRepository.GetByIdAsync(request.AccountTypeId);
            if (accountType == null)
            {
                throw new ArgumentException("Invalid AccountTypeId");
            }

            var moneySource = new MoneySource
            {
                UserId = userId,
                AccountTypeId = request.AccountTypeId,
                Name = request.Name,
                Icon = request.Icon,
                Color = request.Color,
                Balance = request.Balance,
                Currency = request.Currency,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _moneySourceRepository.AddAsync(moneySource);
            
            // Reload với navigation property để đảm bảo AccountType được load đúng cách
            created = await _moneySourceRepository.GetByIdWithAccountTypeAsync(created.Id);
            if (created == null)
            {
                throw new InvalidOperationException("Failed to create money source");
            }
            
            _logger.LogInformation("Created MoneySource {Id} for user {UserId}", created.Id, userId);
            
            return MapToDto(created);
        }

        public async Task<MoneySourceDto?> UpdateAsync(Guid id, Guid userId, UpdateMoneySourceDto request)
        {
            var moneySource = await _moneySourceRepository.GetByIdWithAccountTypeAsync(id);
            
            if (moneySource == null || moneySource.UserId != userId)
            {
                return null;
            }

            // Update fields if provided
            if (request.AccountTypeId.HasValue)
            {
                var accountType = await _accountTypeRepository.GetByIdAsync(request.AccountTypeId.Value);
                if (accountType == null)
                {
                    throw new ArgumentException("Invalid AccountTypeId");
                }
                moneySource.AccountTypeId = request.AccountTypeId.Value;
                moneySource.AccountType = accountType;
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                moneySource.Name = request.Name;
            }

            if (!string.IsNullOrWhiteSpace(request.Icon))
            {
                moneySource.Icon = request.Icon;
            }

            if (!string.IsNullOrWhiteSpace(request.Color))
            {
                moneySource.Color = request.Color;
            }

            // Luôn cập nhật số dư khi client gửi lên (kể cả 0)
            if (request.Balance.HasValue)
            {
                moneySource.Balance = request.Balance.Value;
                _logger.LogInformation("MoneySource {Id} balance set to {Balance}", id, request.Balance.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Currency))
            {
                moneySource.Currency = request.Currency;
            }

            if (request.IsActive.HasValue)
            {
                moneySource.IsActive = request.IsActive.Value;
            }

            moneySource.UpdatedAt = DateTime.UtcNow;

            var updated = await _moneySourceRepository.UpdateAsync(moneySource);
            
            _logger.LogInformation("Updated MoneySource {Id} for user {UserId}", id, userId);
            
            return MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            var moneySource = await _moneySourceRepository.GetByIdAsync(id);
            
            if (moneySource == null || moneySource.UserId != userId)
            {
                return false;
            }

            // Soft delete
            moneySource.IsActive = false;
            moneySource.UpdatedAt = DateTime.UtcNow;
            await _moneySourceRepository.UpdateAsync(moneySource);
            
            _logger.LogInformation("Soft deleted MoneySource {Id} for user {UserId}", id, userId);
            
            return true;
        }

        /// <summary>
        /// Cập nhật balance của MoneySource (được gọi từ TransactionService)
        /// </summary>
        internal async Task UpdateBalanceAsync(Guid moneySourceId, decimal amount, bool isIncome)
        {
            var moneySource = await _moneySourceRepository.GetByIdAsync(moneySourceId);
            if (moneySource == null)
            {
                throw new ArgumentException("MoneySource not found");
            }

            if (isIncome)
            {
                moneySource.Balance += amount;
            }
            else
            {
                moneySource.Balance -= amount;
            }

            moneySource.UpdatedAt = DateTime.UtcNow;
            await _moneySourceRepository.UpdateAsync(moneySource);
        }

        private MoneySourceDto MapToDto(MoneySource entity)
        {
            return new MoneySourceDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                AccountTypeId = entity.AccountTypeId,
                AccountTypeName = entity.AccountType?.Name ?? string.Empty,
                Name = entity.Name,
                Icon = entity.Icon,
                Color = entity.Color,
                Balance = entity.Balance,
                Currency = entity.Currency,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
