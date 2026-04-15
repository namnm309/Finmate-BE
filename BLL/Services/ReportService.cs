using BLL.DTOs.Response;
using DAL.Data;
using DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class ReportService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly FinmateContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            ITransactionRepository transactionRepository,
            FinmateContext context,
            ILogger<ReportService> logger)
        {
            _transactionRepository = transactionRepository;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy báo cáo tổng quan thu/chi và thống kê theo danh mục
        /// </summary>
        public async Task<OverviewReportDto> GetOverviewAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Query transactions với filter
                var query = _context.Transactions
                    .Include(t => t.TransactionType)
                    .Include(t => t.Category)
                    .Where(t => t.UserId == userId && !t.ExcludeFromReport);

                // Apply date filter
                if (startDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    // Set end date to end of day
                    var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(t => t.TransactionDate <= endOfDay);
                }

                var transactions = await query.ToListAsync();

                // Tính tổng thu (IsIncome = true)
                var totalIncome = transactions
                    .Where(t => t.TransactionType.IsIncome)
                    .Sum(t => t.Amount);

                // Tính tổng chi (IsIncome = false)
                var totalExpense = transactions
                    .Where(t => !t.TransactionType.IsIncome)
                    .Sum(t => t.Amount);

                // Tính chênh lệch
                var difference = totalIncome - totalExpense;

                // Nhóm theo Category và tính thống kê (chỉ cho chi tiêu)
                var expenseTransactions = transactions
                    .Where(t => !t.TransactionType.IsIncome)
                    .ToList();

                var categoryStats = expenseTransactions
                    .GroupBy(t => new
                    {
                        t.CategoryId,
                        CategoryName = t.Category?.Name ?? "Không xác định",
                        CategoryIcon = t.Category?.Icon ?? "category",
                        TransactionTypeColor = t.TransactionType?.Color ?? "#6B7280",
                    })
                    .Select(g => new CategoryStatDto
                    {
                        CategoryId = g.Key.CategoryId,
                        CategoryName = g.Key.CategoryName,
                        CategoryIcon = g.Key.CategoryIcon,
                        Amount = g.Sum(t => t.Amount),
                        Percentage = totalExpense > 0 
                            ? (double)(g.Sum(t => t.Amount) / totalExpense * 100) 
                            : 0,
                        Color = g.Key.TransactionTypeColor
                    })
                    .OrderByDescending(c => c.Amount)
                    .ToList();

                return new OverviewReportDto
                {
                    TotalIncome = totalIncome,
                    TotalExpense = totalExpense,
                    Difference = difference,
                    CategoryStats = categoryStats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overview report for user {UserId}", userId);
                throw;
            }
        }
    }
}
