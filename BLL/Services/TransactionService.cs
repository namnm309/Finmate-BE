using BLL.DTOs.Request;
using BLL.DTOs.Response;
using DAL.Data;
using DAL.Models;
using DAL.Repositories;
using FinmateController.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class TransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ITransactionTypeRepository _transactionTypeRepository;
        private readonly IMoneySourceRepository _moneySourceRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IContactRepository _contactRepository;
        private readonly FinmateContext _context;
        private readonly IHubContext<TransactionHub> _transactionHubContext;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(
            ITransactionRepository transactionRepository,
            ITransactionTypeRepository transactionTypeRepository,
            IMoneySourceRepository moneySourceRepository,
            ICategoryRepository categoryRepository,
            IContactRepository contactRepository,
            FinmateContext context,
            ILogger<TransactionService> logger,
            IHubContext<TransactionHub> transactionHubContext)
        {
            _transactionRepository = transactionRepository;
            _transactionTypeRepository = transactionTypeRepository;
            _moneySourceRepository = moneySourceRepository;
            _categoryRepository = categoryRepository;
            _contactRepository = contactRepository;
            _context = context;
            _logger = logger;
            _transactionHubContext = transactionHubContext;
        }

        public async Task<TransactionListResponseDto> GetAllByUserIdAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            var transactions = await _transactionRepository.GetAllByUserIdAsync(userId, page, pageSize);
            var totalCount = await _transactionRepository.CountByUserIdAsync(userId);

            return new TransactionListResponseDto
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Transactions = transactions.Select(MapToDto).ToList()
            };
        }

        public async Task<TransactionListResponseDto> GetWithFilterAsync(
            Guid userId,
            Guid? transactionTypeId = null,
            Guid? categoryId = null,
            Guid? moneySourceId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 20)
        {
            var transactions = await _transactionRepository.GetByUserIdWithFilterAsync(
                userId, transactionTypeId, categoryId, moneySourceId, startDate, endDate, page, pageSize);
            var totalCount = await _transactionRepository.CountByUserIdAsync(userId);

            return new TransactionListResponseDto
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Transactions = transactions.Select(MapToDto).ToList()
            };
        }

        public async Task<TransactionDto?> GetByIdAsync(Guid id, Guid userId)
        {
            var transaction = await _transactionRepository.GetByIdWithDetailsAsync(id);

            if (transaction == null || transaction.UserId != userId)
            {
                return null;
            }

            return MapToDto(transaction);
        }

        public async Task<TransactionDto> CreateAsync(Guid userId, CreateTransactionDto request)
        {
            // Validate TransactionType
            var transactionType = await _transactionTypeRepository.GetByIdAsync(request.TransactionTypeId);
            if (transactionType == null)
            {
                throw new ArgumentException("Invalid TransactionTypeId");
            }

            // Validate MoneySource
            var moneySource = await _moneySourceRepository.GetByIdAsync(request.MoneySourceId);
            if (moneySource == null || moneySource.UserId != userId)
            {
                throw new ArgumentException("Invalid MoneySourceId");
            }
            if (!moneySource.IsActive)
            {
                throw new ArgumentException("MoneySource is not active");
            }

            // Validate Category
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
            if (category == null || category.UserId != userId)
            {
                throw new ArgumentException("Invalid CategoryId");
            }
            if (!category.IsActive)
            {
                throw new ArgumentException("Category is not active");
            }

            // Validate Contact (optional)
            Contact? contact = null;
            if (request.ContactId.HasValue)
            {
                contact = await _contactRepository.GetByIdAsync(request.ContactId.Value);
                if (contact == null || contact.UserId != userId)
                {
                    throw new ArgumentException("Invalid ContactId");
                }
            }

            // Validate balance (không cho số dư âm trừ thẻ tín dụng)
            if (!transactionType.IsIncome)
            {
                // Kiểm tra nếu chi tiền và số dư không đủ
                // Có thể bỏ qua validation này nếu muốn cho phép số dư âm
                // hoặc kiểm tra loại tài khoản là thẻ tín dụng
            }

            // Use database transaction để đảm bảo tính toàn vẹn
            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create transaction
                var transaction = new Transaction
                {
                    UserId = userId,
                    TransactionTypeId = request.TransactionTypeId,
                    MoneySourceId = request.MoneySourceId,
                    CategoryId = request.CategoryId,
                    ContactId = request.ContactId,
                    Amount = request.Amount,
                    TransactionDate = request.TransactionDate,
                    Description = request.Description,
                    IsBorrowingForThis = request.IsBorrowingForThis,
                    IsFee = request.IsFee,
                    ExcludeFromReport = request.ExcludeFromReport,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Update MoneySource balance
                if (transactionType.IsIncome)
                {
                    moneySource.Balance += request.Amount;
                }
                else
                {
                    moneySource.Balance -= request.Amount;
                }
                moneySource.UpdatedAt = DateTime.UtcNow;

                _context.Transactions.Add(transaction);
                _context.MoneySources.Update(moneySource);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation(
                    "Created Transaction {Id} for user {UserId}. Type: {Type}, Amount: {Amount}, MoneySource: {MoneySource}",
                    transaction.Id, userId, transactionType.Name, request.Amount, moneySource.Name);

                // Load navigation properties for response
                transaction.TransactionType = transactionType;
                transaction.MoneySource = moneySource;
                transaction.Category = category;
                transaction.Contact = contact;

                // Notify SignalR clients for this user
                await _transactionHubContext.Clients
                    .Group($"user:{userId}")
                    .SendAsync("TransactionsUpdated", new
                    {
                        transactionId = transaction.Id,
                        action = "created"
                    });

                return MapToDto(transaction);
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Error creating transaction for user {UserId}", userId);
                throw;
            }
        }

        public async Task<TransactionDto?> UpdateAsync(Guid id, Guid userId, UpdateTransactionDto request)
        {
            var transaction = await _transactionRepository.GetByIdWithDetailsAsync(id);

            if (transaction == null || transaction.UserId != userId)
            {
                return null;
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var oldTransactionType = transaction.TransactionType;
                var oldMoneySource = transaction.MoneySource;
                var oldAmount = transaction.Amount;

                // Rollback old balance
                if (oldTransactionType.IsIncome)
                {
                    oldMoneySource.Balance -= oldAmount;
                }
                else
                {
                    oldMoneySource.Balance += oldAmount;
                }

                // Update transaction fields
                TransactionType? newTransactionType = oldTransactionType;
                if (request.TransactionTypeId.HasValue && request.TransactionTypeId.Value != transaction.TransactionTypeId)
                {
                    newTransactionType = await _transactionTypeRepository.GetByIdAsync(request.TransactionTypeId.Value);
                    if (newTransactionType == null)
                    {
                        throw new ArgumentException("Invalid TransactionTypeId");
                    }
                    transaction.TransactionTypeId = request.TransactionTypeId.Value;
                    transaction.TransactionType = newTransactionType;
                }

                MoneySource? newMoneySource = oldMoneySource;
                if (request.MoneySourceId.HasValue && request.MoneySourceId.Value != transaction.MoneySourceId)
                {
                    newMoneySource = await _moneySourceRepository.GetByIdAsync(request.MoneySourceId.Value);
                    if (newMoneySource == null || newMoneySource.UserId != userId)
                    {
                        throw new ArgumentException("Invalid MoneySourceId");
                    }
                    transaction.MoneySourceId = request.MoneySourceId.Value;
                    transaction.MoneySource = newMoneySource;
                }

                if (request.CategoryId.HasValue)
                {
                    var category = await _categoryRepository.GetByIdAsync(request.CategoryId.Value);
                    if (category == null || category.UserId != userId)
                    {
                        throw new ArgumentException("Invalid CategoryId");
                    }
                    transaction.CategoryId = request.CategoryId.Value;
                    transaction.Category = category;
                }

                if (request.ContactId.HasValue)
                {
                    var contact = await _contactRepository.GetByIdAsync(request.ContactId.Value);
                    if (contact == null || contact.UserId != userId)
                    {
                        throw new ArgumentException("Invalid ContactId");
                    }
                    transaction.ContactId = request.ContactId.Value;
                }
                else if (request.ContactId == null)
                {
                    // Cho phép clear contact
                    transaction.ContactId = null;
                }

                var newAmount = request.Amount ?? oldAmount;
                if (request.Amount.HasValue)
                {
                    transaction.Amount = request.Amount.Value;
                }

                if (request.TransactionDate.HasValue)
                {
                    transaction.TransactionDate = request.TransactionDate.Value;
                }

                if (request.Description != null)
                {
                    transaction.Description = request.Description;
                }

                if (request.IsBorrowingForThis.HasValue)
                {
                    transaction.IsBorrowingForThis = request.IsBorrowingForThis.Value;
                }

                if (request.IsFee.HasValue)
                {
                    transaction.IsFee = request.IsFee.Value;
                }

                if (request.ExcludeFromReport.HasValue)
                {
                    transaction.ExcludeFromReport = request.ExcludeFromReport.Value;
                }

                transaction.UpdatedAt = DateTime.UtcNow;

                // Apply new balance
                if (newTransactionType.IsIncome)
                {
                    newMoneySource.Balance += newAmount;
                }
                else
                {
                    newMoneySource.Balance -= newAmount;
                }

                // Update both money sources if changed
                oldMoneySource.UpdatedAt = DateTime.UtcNow;
                _context.MoneySources.Update(oldMoneySource);

                if (newMoneySource.Id != oldMoneySource.Id)
                {
                    newMoneySource.UpdatedAt = DateTime.UtcNow;
                    _context.MoneySources.Update(newMoneySource);
                }

                _context.Transactions.Update(transaction);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation("Updated Transaction {Id} for user {UserId}", id, userId);

                // Notify SignalR clients for this user
                await _transactionHubContext.Clients
                    .Group($"user:{userId}")
                    .SendAsync("TransactionsUpdated", new
                    {
                        transactionId = transaction.Id,
                        action = "updated"
                    });

                return MapToDto(transaction);
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Error updating transaction {Id} for user {UserId}", id, userId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            var transaction = await _transactionRepository.GetByIdWithDetailsAsync(id);

            if (transaction == null || transaction.UserId != userId)
            {
                return false;
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Rollback balance
                var moneySource = transaction.MoneySource;
                if (transaction.TransactionType.IsIncome)
                {
                    moneySource.Balance -= transaction.Amount;
                }
                else
                {
                    moneySource.Balance += transaction.Amount;
                }
                moneySource.UpdatedAt = DateTime.UtcNow;

                _context.MoneySources.Update(moneySource);
                _context.Transactions.Remove(transaction);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation(
                    "Deleted Transaction {Id} for user {UserId}. Rolled back balance: {Amount}",
                    id, userId, transaction.Amount);

                // Notify SignalR clients for this user
                await _transactionHubContext.Clients
                    .Group($"user:{userId}")
                    .SendAsync("TransactionsUpdated", new
                    {
                        transactionId = transaction.Id,
                        action = "deleted"
                    });

                return true;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting transaction {Id} for user {UserId}", id, userId);
                throw;
            }
        }

        private TransactionDto MapToDto(Transaction entity)
        {
            return new TransactionDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                TransactionTypeId = entity.TransactionTypeId,
                TransactionTypeName = entity.TransactionType?.Name ?? string.Empty,
                TransactionTypeColor = entity.TransactionType?.Color ?? string.Empty,
                IsIncome = entity.TransactionType?.IsIncome ?? false,
                MoneySourceId = entity.MoneySourceId,
                MoneySourceName = entity.MoneySource?.Name ?? string.Empty,
                MoneySourceIcon = entity.MoneySource?.Icon ?? string.Empty,
                CategoryId = entity.CategoryId,
                CategoryName = entity.Category?.Name ?? string.Empty,
                CategoryIcon = entity.Category?.Icon ?? string.Empty,
                ContactId = entity.ContactId,
                ContactName = entity.Contact?.Name,
                Amount = entity.Amount,
                TransactionDate = entity.TransactionDate,
                Description = entity.Description,
                IsBorrowingForThis = entity.IsBorrowingForThis,
                IsFee = entity.IsFee,
                ExcludeFromReport = entity.ExcludeFromReport,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
