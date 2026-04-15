using BLL.DTOs.Response;
using BLL.Services.Ai;
using DAL.Data;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace BLL.Services
{
    public enum AiFeatureKind
    {
        Chat = 0,
        Plan = 1,
    }

    public sealed class AiUsageService
    {
        private readonly FinmateContext _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiUsageService> _logger;

        public AiUsageService(
            FinmateContext db,
            IConfiguration configuration,
            ILogger<AiUsageService> logger)
        {
            _db = db;
            _configuration = configuration;
            _logger = logger;
        }

        private static string PeriodKeyNowUtc() =>
            DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);

        private (int planLimit, int chatLimit) ResolveLimits(bool isPremium)
        {
            var s = _configuration.GetSection("AIUsage");
            if (isPremium)
            {
                return (
                    s.GetValue("PremiumPlanCallsPerMonth", 300),
                    s.GetValue("PremiumChatCallsPerMonth", 1000));
            }

            return (
                s.GetValue("FreePlanCallsPerMonth", 5),
                s.GetValue("FreeChatCallsPerMonth", 20));
        }

        public async Task<bool> IsUserPremiumAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.IsPremium)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<AiUsageSnapshotDto> GetSnapshotAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var isPremium = await IsUserPremiumAsync(userId, cancellationToken);
            var period = PeriodKeyNowUtc();
            var row = await _db.UserAiMonthlyUsages.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.PeriodKey == period, cancellationToken);

            var (planLim, chatLim) = ResolveLimits(isPremium);

            return new AiUsageSnapshotDto
            {
                PeriodKey = period,
                PlanCallsUsed = row?.PlanCalls ?? 0,
                PlanCallsLimit = planLim,
                ChatCallsUsed = row?.ChatCalls ?? 0,
                ChatCallsLimit = chatLim,
                IsPremium = isPremium,
            };
        }

        public async Task EnsureCanCallAsync(Guid userId, AiFeatureKind feature, CancellationToken cancellationToken = default)
        {
            var isPremium = await IsUserPremiumAsync(userId, cancellationToken);
            var (planLim, chatLim) = ResolveLimits(isPremium);
            var period = PeriodKeyNowUtc();

            var row = await _db.UserAiMonthlyUsages.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.PeriodKey == period, cancellationToken);

            var planUsed = row?.PlanCalls ?? 0;
            var chatUsed = row?.ChatCalls ?? 0;

            if (feature == AiFeatureKind.Plan && planUsed >= planLim)
            {
                throw new AiQuotaExceededException(
                    "Het luot AI lap ke hoach trong thang " + period
                    + ". Nang cap Premium de tang han muc (cau hinh AIUsage tren server).");
            }

            if (feature == AiFeatureKind.Chat && chatUsed >= chatLim)
            {
                throw new AiQuotaExceededException(
                    "Het luot AI chatbot trong thang " + period
                    + ". Nang cap Premium de tang han muc (cau hinh AIUsage tren server).");
            }
        }

        public async Task IncrementAsync(Guid userId, AiFeatureKind feature, CancellationToken cancellationToken = default)
        {
            var period = PeriodKeyNowUtc();
            var now = DateTime.UtcNow;

            var row = await _db.UserAiMonthlyUsages
                .FirstOrDefaultAsync(x => x.UserId == userId && x.PeriodKey == period, cancellationToken);

            if (row == null)
            {
                row = new UserAiMonthlyUsage
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PeriodKey = period,
                    PlanCalls = 0,
                    ChatCalls = 0,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                _db.UserAiMonthlyUsages.Add(row);
            }

            if (feature == AiFeatureKind.Plan)
                row.PlanCalls++;
            else
                row.ChatCalls++;

            row.UpdatedAt = now;

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "AI usage increment UserId={UserId} Period={Period} Feature={Feature} PlanCalls={Plan} ChatCalls={Chat}",
                userId, period, feature, row.PlanCalls, row.ChatCalls);
        }
    }
}
