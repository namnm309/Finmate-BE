using BLL.DTOs.Request;
using BLL.DTOs.Response;
using DAL.Models;
using DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class CategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITransactionTypeRepository _transactionTypeRepository;
        private readonly ILogger<CategoryService> _logger;

        // Default categories sẽ được tạo cho user mới
        private static readonly List<(string Name, string Icon, int Order)> DefaultExpenseCategories = new()
        {
            ("Gửi xe", "local-parking", 1),
            ("Học hành", "school", 2),
            ("Shoppe/Tiki", "shopping-bag", 3),
            ("Vui chơi giải trí", "sports-esports", 4),
            ("Ăn uống", "restaurant", 5),
            ("Dịch vụ sinh hoạt", "home", 6),
            ("Xăng xe", "local-gas-station", 7),
            ("Y tế", "local-hospital", 8),
            ("Mua sắm", "shopping-cart", 9),
            ("Du lịch", "flight", 10),
            ("Quà tặng", "card-giftcard", 11),
            ("Điện thoại", "phone", 12),
            ("Internet", "wifi", 13),
            ("Bảo hiểm", "security", 14),
            ("Thuế", "receipt", 15)
        };

        private static readonly List<(string Name, string Icon, int Order)> DefaultIncomeCategories = new()
        {
            ("Lương", "payments", 1),
            ("Thưởng", "card-giftcard", 2),
            ("Đầu tư", "trending-up", 3),
            ("Kinh doanh", "store", 4),
            ("Khác", "more-horiz", 5)
        };

        public CategoryService(
            ICategoryRepository categoryRepository,
            ITransactionTypeRepository transactionTypeRepository,
            ILogger<CategoryService> logger)
        {
            _categoryRepository = categoryRepository;
            _transactionTypeRepository = transactionTypeRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllByUserIdAsync(Guid userId)
        {
            // Auto-seed categories nếu user chưa có
            await EnsureUserHasCategoriesAsync(userId);

            var categories = await _categoryRepository.GetAllByUserIdAsync(userId);
            return categories.Select(MapToDto);
        }

        public async Task<IEnumerable<CategoryDto>> GetActiveByUserIdAsync(Guid userId)
        {
            await EnsureUserHasCategoriesAsync(userId);

            var categories = await _categoryRepository.GetActiveByUserIdAsync(userId);
            return categories.Select(MapToDto);
        }

        public async Task<IEnumerable<CategoryDto>> GetByTransactionTypeAsync(Guid userId, Guid transactionTypeId)
        {
            await EnsureUserHasCategoriesAsync(userId);

            var categories = await _categoryRepository.GetByUserIdAndTransactionTypeAsync(userId, transactionTypeId);
            return categories.Select(MapToDto);
        }

        public async Task<CategoryDto?> GetByIdAsync(Guid id, Guid userId)
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null || category.UserId != userId)
            {
                return null;
            }

            return MapToDto(category);
        }

        public async Task<CategoryDto> CreateAsync(Guid userId, CreateCategoryDto request)
        {
            // Validate TransactionType exists
            var transactionType = await _transactionTypeRepository.GetByIdAsync(request.TransactionTypeId);
            if (transactionType == null)
            {
                throw new ArgumentException("Invalid TransactionTypeId");
            }

            // Check duplicate name
            if (await _categoryRepository.ExistsByNameForUserAsync(userId, request.Name))
            {
                throw new ArgumentException("Category with this name already exists");
            }

            var category = new Category
            {
                UserId = userId,
                TransactionTypeId = request.TransactionTypeId,
                Name = request.Name,
                Icon = request.Icon,
                DisplayOrder = request.DisplayOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _categoryRepository.AddAsync(category);
            created.TransactionType = transactionType;

            _logger.LogInformation("Created Category {Id} for user {UserId}", created.Id, userId);

            return MapToDto(created);
        }

        public async Task<CategoryDto?> UpdateAsync(Guid id, Guid userId, UpdateCategoryDto request)
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null || category.UserId != userId)
            {
                return null;
            }

            // Update fields if provided
            if (request.TransactionTypeId.HasValue)
            {
                var transactionType = await _transactionTypeRepository.GetByIdAsync(request.TransactionTypeId.Value);
                if (transactionType == null)
                {
                    throw new ArgumentException("Invalid TransactionTypeId");
                }
                category.TransactionTypeId = request.TransactionTypeId.Value;
                category.TransactionType = transactionType;
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                // Check duplicate name (exclude current category)
                if (await _categoryRepository.ExistsByNameForUserAsync(userId, request.Name, id))
                {
                    throw new ArgumentException("Category with this name already exists");
                }
                category.Name = request.Name;
            }

            if (!string.IsNullOrWhiteSpace(request.Icon))
            {
                category.Icon = request.Icon;
            }

            if (request.DisplayOrder.HasValue)
            {
                category.DisplayOrder = request.DisplayOrder.Value;
            }

            if (request.IsActive.HasValue)
            {
                category.IsActive = request.IsActive.Value;
            }

            category.UpdatedAt = DateTime.UtcNow;

            var updated = await _categoryRepository.UpdateAsync(category);

            _logger.LogInformation("Updated Category {Id} for user {UserId}", id, userId);

            return MapToDto(updated);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(Guid id, Guid userId)
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null || category.UserId != userId)
            {
                return (false, "Category not found");
            }

            // Check if category has transactions
            if (await _categoryRepository.HasTransactionsAsync(id))
            {
                // Soft delete instead of hard delete
                category.IsActive = false;
                category.UpdatedAt = DateTime.UtcNow;
                await _categoryRepository.UpdateAsync(category);

                _logger.LogInformation("Soft deleted Category {Id} for user {UserId} (has transactions)", id, userId);
                return (true, null);
            }

            // Hard delete
            await _categoryRepository.DeleteAsync(id);

            _logger.LogInformation("Hard deleted Category {Id} for user {UserId}", id, userId);

            return (true, null);
        }

        /// <summary>
        /// Đảm bảo user có bộ categories mặc định
        /// </summary>
        private async Task EnsureUserHasCategoriesAsync(Guid userId)
        {
            var count = await _categoryRepository.CountByUserIdAsync(userId);
            if (count > 0)
            {
                return; // User đã có categories
            }

            _logger.LogInformation("Auto-seeding categories for new user {UserId}", userId);

            var transactionTypes = await _transactionTypeRepository.GetAllAsync();
            var expenseType = transactionTypes.FirstOrDefault(t => t.Name == "Chi tiêu");
            var incomeType = transactionTypes.FirstOrDefault(t => t.Name == "Thu tiền");

            var categories = new List<Category>();

            // Tạo expense categories
            if (expenseType != null)
            {
                foreach (var (name, icon, order) in DefaultExpenseCategories)
                {
                    categories.Add(new Category
                    {
                        UserId = userId,
                        TransactionTypeId = expenseType.Id,
                        Name = name,
                        Icon = icon,
                        DisplayOrder = order,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            // Tạo income categories
            if (incomeType != null)
            {
                foreach (var (name, icon, order) in DefaultIncomeCategories)
                {
                    categories.Add(new Category
                    {
                        UserId = userId,
                        TransactionTypeId = incomeType.Id,
                        Name = name,
                        Icon = icon,
                        DisplayOrder = order + 100, // Offset để tránh trùng DisplayOrder
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            if (categories.Any())
            {
                await _categoryRepository.AddRangeAsync(categories);
                _logger.LogInformation("Created {Count} default categories for user {UserId}", categories.Count, userId);
            }
        }

        private CategoryDto MapToDto(Category entity)
        {
            return new CategoryDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                TransactionTypeId = entity.TransactionTypeId,
                TransactionTypeName = entity.TransactionType?.Name ?? string.Empty,
                Name = entity.Name,
                Icon = entity.Icon,
                IsActive = entity.IsActive,
                DisplayOrder = entity.DisplayOrder,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
