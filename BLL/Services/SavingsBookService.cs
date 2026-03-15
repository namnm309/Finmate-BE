using BLL.DTOs.Request;
using BLL.DTOs.Response;
using DAL.Data;
using DAL.Models;
using DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class SavingsBookService
    {
        private readonly ISavingsBookRepository _savingsBookRepository;
        private readonly IBankRepository _bankRepository;
        private readonly IMoneySourceRepository _moneySourceRepository;
        private readonly ITransactionTypeRepository _transactionTypeRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly FinmateContext _context;
        private readonly ILogger<SavingsBookService> _logger;

        private static readonly Guid ThuTienTransactionTypeId = Guid.Parse("22222222-2222-2222-2222-222222222002");

        public SavingsBookService(
            ISavingsBookRepository savingsBookRepository,
            IBankRepository bankRepository,
            IMoneySourceRepository moneySourceRepository,
            ITransactionTypeRepository transactionTypeRepository,
            ICategoryRepository categoryRepository,
            ITransactionRepository transactionRepository,
            FinmateContext context,
            ILogger<SavingsBookService> logger)
        {
            _savingsBookRepository = savingsBookRepository;
            _bankRepository = bankRepository;
            _moneySourceRepository = moneySourceRepository;
            _transactionTypeRepository = transactionTypeRepository;
            _categoryRepository = categoryRepository;
            _transactionRepository = transactionRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<SavingsBookDto>> GetByUserIdAsync(Guid userId)
        {
            var list = await _savingsBookRepository.GetByUserIdAsync(userId);
            return list.Select(MapToDto);
        }

        public async Task<SavingsBookDto?> GetByIdAsync(Guid id, Guid userId)
        {
            var entity = await _savingsBookRepository.GetByIdWithBankAsync(id);
            if (entity == null || entity.UserId != userId)
                return null;
            return MapToDto(entity);
        }

        public async Task<SavingsBookDto> CreateAsync(Guid userId, CreateSavingsBookDto request)
        {
            var bank = await _bankRepository.GetByIdAsync(request.BankId);
            if (bank == null)
                throw new ArgumentException("Invalid BankId");

            if (request.SourceMoneySourceId.HasValue)
            {
                var source = await _moneySourceRepository.GetByIdAsync(request.SourceMoneySourceId.Value);
                if (source == null || source.UserId != userId)
                    throw new ArgumentException("Invalid SourceMoneySourceId");
            }

            var maturityDate = request.DepositDate.AddMonths(request.TermMonths);

            var entity = new SavingsBook
            {
                UserId = userId,
                BankId = request.BankId,
                Name = request.Name,
                Currency = request.Currency,
                DepositDate = request.DepositDate,
                TermMonths = request.TermMonths,
                InterestRate = request.InterestRate,
                NonTermInterestRate = request.NonTermInterestRate,
                DaysInYearForInterest = request.DaysInYearForInterest,
                InterestPaymentType = request.InterestPaymentType,
                MaturityOption = request.MaturityOption,
                SourceMoneySourceId = request.SourceMoneySourceId,
                Description = request.Description,
                ExcludeFromReports = request.ExcludeFromReports,
                InitialBalance = request.InitialBalance,
                CurrentBalance = request.InitialBalance,
                MaturityDate = maturityDate,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            entity = await _savingsBookRepository.AddAsync(entity);
            entity.Bank = bank;
            return MapToDto(entity);
        }

        public async Task<SavingsBookDto?> UpdateAsync(Guid id, Guid userId, UpdateSavingsBookDto request)
        {
            var entity = await _savingsBookRepository.GetByIdWithBankAsync(id);
            if (entity == null || entity.UserId != userId)
                return null;
            if (entity.Status != "Active")
                throw new InvalidOperationException("Chỉ có thể sửa sổ đang hoạt động");

            if (request.BankId.HasValue)
            {
                var bank = await _bankRepository.GetByIdAsync(request.BankId.Value);
                if (bank == null)
                    throw new ArgumentException("Invalid BankId");
                entity.BankId = request.BankId.Value;
                entity.Bank = bank;
            }

            if (request.Name != null) entity.Name = request.Name;
            if (request.Currency != null) entity.Currency = request.Currency;
            if (request.DepositDate.HasValue) entity.DepositDate = request.DepositDate.Value;
            if (request.TermMonths.HasValue) entity.TermMonths = request.TermMonths.Value;
            if (request.InterestRate.HasValue) entity.InterestRate = request.InterestRate.Value;
            if (request.NonTermInterestRate.HasValue) entity.NonTermInterestRate = request.NonTermInterestRate.Value;
            if (request.DaysInYearForInterest.HasValue) entity.DaysInYearForInterest = request.DaysInYearForInterest.Value;
            if (request.InterestPaymentType != null) entity.InterestPaymentType = request.InterestPaymentType;
            if (request.MaturityOption != null) entity.MaturityOption = request.MaturityOption;
            if (request.SourceMoneySourceId.HasValue) entity.SourceMoneySourceId = request.SourceMoneySourceId.Value;
            if (request.Description != null) entity.Description = request.Description;
            if (request.ExcludeFromReports.HasValue) entity.ExcludeFromReports = request.ExcludeFromReports.Value;

            entity.MaturityDate = entity.DepositDate.AddMonths(entity.TermMonths);
            entity.UpdatedAt = DateTime.UtcNow;

            entity = await _savingsBookRepository.UpdateAsync(entity);
            return MapToDto(entity);
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            var entity = await _savingsBookRepository.GetByIdAsync(id);
            if (entity == null || entity.UserId != userId)
                return false;
            return await _savingsBookRepository.DeleteAsync(id);
        }

        public async Task<SavingsBookDto?> DepositAsync(Guid id, Guid userId, DepositSavingsBookDto request)
        {
            var entity = await _savingsBookRepository.GetByIdWithBankAsync(id);
            if (entity == null || entity.UserId != userId)
                return null;
            if (entity.Status != "Active")
                throw new InvalidOperationException("Chỉ có thể gửi thêm vào sổ đang hoạt động");

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (request.SourceMoneySourceId.HasValue)
                {
                    var source = await _moneySourceRepository.GetByIdAsync(request.SourceMoneySourceId.Value);
                    if (source == null || source.UserId != userId)
                        throw new ArgumentException("Invalid SourceMoneySourceId");
                    if (source.Balance < request.Amount)
                        throw new InvalidOperationException("Số dư tài khoản nguồn không đủ");
                    source.Balance -= request.Amount;
                    source.UpdatedAt = DateTime.UtcNow;
                    _context.MoneySources.Update(source);
                }

                entity.CurrentBalance += request.Amount;
                entity.UpdatedAt = DateTime.UtcNow;
                entity = await _savingsBookRepository.UpdateAsync(entity);
                await dbTransaction.CommitAsync();
                return MapToDto(entity);
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<SavingsBookDto?> WithdrawAsync(Guid id, Guid userId, WithdrawSavingsBookDto request)
        {
            var entity = await _savingsBookRepository.GetByIdWithBankAsync(id);
            if (entity == null || entity.UserId != userId)
                return null;
            if (entity.Status != "Active")
                throw new InvalidOperationException("Chỉ có thể rút từ sổ đang hoạt động");
            if (request.Amount > entity.CurrentBalance)
                throw new InvalidOperationException("Số tiền rút vượt quá số dư sổ");

            var dest = await _moneySourceRepository.GetByIdAsync(request.DestinationMoneySourceId);
            if (dest == null || dest.UserId != userId)
                throw new ArgumentException("Invalid DestinationMoneySourceId");

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                dest.Balance += request.Amount;
                dest.UpdatedAt = DateTime.UtcNow;
                _context.MoneySources.Update(dest);

                entity.CurrentBalance -= request.Amount;
                if (entity.CurrentBalance <= 0)
                    entity.Status = "Closed";
                entity.UpdatedAt = DateTime.UtcNow;
                entity = await _savingsBookRepository.UpdateAsync(entity);

                await CreateIncomeTransactionIfPossible(userId, request.DestinationMoneySourceId, request.Amount,
                    request.Date ?? DateTime.UtcNow, $"Rút từ sổ tiết kiệm: {entity.Name}");

                await dbTransaction.CommitAsync();
                return MapToDto(entity);
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<SavingsBookDto?> SettleAsync(Guid id, Guid userId, SettleSavingsBookDto request)
        {
            var entity = await _savingsBookRepository.GetByIdWithBankAsync(id);
            if (entity == null || entity.UserId != userId)
                return null;
            if (entity.Status != "Active")
                throw new InvalidOperationException("Chỉ có thể tất toán sổ đang hoạt động");

            var dest = await _moneySourceRepository.GetByIdAsync(request.DestinationMoneySourceId);
            if (dest == null || dest.UserId != userId)
                throw new ArgumentException("Invalid DestinationMoneySourceId");

            var amount = request.Amount ?? entity.CurrentBalance;
            if (amount <= 0) amount = entity.CurrentBalance;
            if (amount > entity.CurrentBalance)
                throw new InvalidOperationException("Số tiền tất toán vượt quá số dư sổ");

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                dest.Balance += amount;
                dest.UpdatedAt = DateTime.UtcNow;
                _context.MoneySources.Update(dest);

                entity.CurrentBalance = 0;
                entity.Status = "Closed";
                entity.UpdatedAt = DateTime.UtcNow;
                entity = await _savingsBookRepository.UpdateAsync(entity);

                await CreateIncomeTransactionIfPossible(userId, request.DestinationMoneySourceId, amount,
                    request.SettlementDate, $"Tất toán sổ tiết kiệm: {entity.Name}");

                await dbTransaction.CommitAsync();
                return MapToDto(entity);
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        private async Task CreateIncomeTransactionIfPossible(Guid userId, Guid moneySourceId, decimal amount, DateTime date, string description)
        {
            try
            {
                var incomeType = await _transactionTypeRepository.GetByIdAsync(ThuTienTransactionTypeId);
                if (incomeType == null) return;

                var incomeCategories = await _categoryRepository.GetByUserIdAndTransactionTypeAsync(userId, ThuTienTransactionTypeId);
                var category = incomeCategories.FirstOrDefault();
                if (category == null) return;

                var transaction = new Transaction
                {
                    UserId = userId,
                    TransactionTypeId = ThuTienTransactionTypeId,
                    MoneySourceId = moneySourceId,
                    CategoryId = category.Id,
                    Amount = amount,
                    TransactionDate = date,
                    Description = description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not create income transaction for savings settlement");
            }
        }

        private static SavingsBookDto MapToDto(SavingsBook entity)
        {
            return new SavingsBookDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                BankId = entity.BankId,
                BankName = entity.Bank?.Name ?? string.Empty,
                Name = entity.Name,
                Currency = entity.Currency,
                DepositDate = entity.DepositDate,
                TermMonths = entity.TermMonths,
                InterestRate = entity.InterestRate,
                NonTermInterestRate = entity.NonTermInterestRate,
                DaysInYearForInterest = entity.DaysInYearForInterest,
                InterestPaymentType = entity.InterestPaymentType,
                MaturityOption = entity.MaturityOption,
                SourceMoneySourceId = entity.SourceMoneySourceId,
                Description = entity.Description,
                ExcludeFromReports = entity.ExcludeFromReports,
                InitialBalance = entity.InitialBalance,
                CurrentBalance = entity.CurrentBalance,
                MaturityDate = entity.MaturityDate,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
