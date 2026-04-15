using BLL.DTOs.Response;
using DAL.Data;
using DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

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

        /// <summary>
        /// Chi tieu theo tung ngay: tuan hien tai vs tuan truoc (UTC, Chu nhat = ngay dau tuan).
        /// Gia tri la tong Amount (don vi tien trong DB, thuong VND).
        /// </summary>
        public async Task<List<WeeklyExpenseBarDto>> GetWeeklyExpenseComparisonAsync(Guid userId)
        {
            var todayUtc = DateTime.UtcNow.Date;
            var daysFromSunday = (int)todayUtc.DayOfWeek;
            var thisWeekStart = todayUtc.AddDays(-daysFromSunday);
            var lastWeekStart = thisWeekStart.AddDays(-7);
            var rangeStart = lastWeekStart;
            var rangeEndExclusive = thisWeekStart.AddDays(7);
            var en = CultureInfo.GetCultureInfo("en-US");

            var expenseRows = await _context.Transactions.AsNoTracking()
                .Where(t => t.UserId == userId && !t.ExcludeFromReport
                    && t.TransactionDate >= rangeStart && t.TransactionDate < rangeEndExclusive)
                .Join(_context.TransactionTypes.AsNoTracking(),
                    t => t.TransactionTypeId,
                    tt => tt.Id,
                    (t, tt) => new { t.TransactionDate, t.Amount, tt.IsIncome })
                .Where(x => !x.IsIncome)
                .Select(x => new { x.TransactionDate, x.Amount })
                .ToListAsync();

            var list = new List<WeeklyExpenseBarDto>();
            for (var i = 0; i < 7; i++)
            {
                var dThis = thisWeekStart.AddDays(i);
                var dLast = lastWeekStart.AddDays(i);
                var thisSum = expenseRows
                    .Where(t => t.TransactionDate >= dThis && t.TransactionDate < dThis.AddDays(1))
                    .Sum(t => t.Amount);
                var lastSum = expenseRows
                    .Where(t => t.TransactionDate >= dLast && t.TransactionDate < dLast.AddDays(1))
                    .Sum(t => t.Amount);
                var label = $"{dThis.Day} {dThis.ToString("ddd", en)}";
                list.Add(new WeeklyExpenseBarDto
                {
                    Day = label,
                    ThisWeek = (double)thisSum,
                    LastWeek = (double)lastSum,
                });
            }

            return list;
        }
    }
}
