using BLL.DTOs.Request;
using BLL.DTOs.Response;
using DAL.Models;
using DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class GoalService
    {
        private readonly IGoalRepository _goalRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GoalService> _logger;

        public GoalService(
            IGoalRepository goalRepository,
            IUserRepository userRepository,
            ILogger<GoalService> logger)
        {
            _goalRepository = goalRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<List<GoalDto>> GetAllByUserIdAsync(Guid userId)
        {
            var goals = await _goalRepository.GetByUserIdAsync(userId);
            return goals.Select(MapToDto).ToList();
        }

        public async Task<GoalDto?> GetByIdAsync(Guid id, Guid userId)
        {
            var goal = await _goalRepository.GetByIdAsync(id);

            if (goal == null || goal.UserId != userId)
            {
                return null;
            }

            return MapToDto(goal);
        }

        public async Task<GoalDto> CreateAsync(Guid userId, CreateGoalDto request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            var goal = new Goal
            {
                UserId = userId,
                Title = request.Title,
                Description = request.Description,
                TargetAmount = request.TargetAmount,
                CurrentAmount = request.CurrentAmount,
                TargetDate = request.TargetDate,
                Currency = request.Currency ?? "VND",
                Icon = request.Icon ?? "flag",
                Color = request.Color ?? "#51A2FF",
                Status = "Active",
                IsActive = true
            };

            var createdGoal = await _goalRepository.AddAsync(goal);
            return MapToDto(createdGoal);
        }

        public async Task<GoalDto?> UpdateAsync(Guid id, Guid userId, UpdateGoalDto request)
        {
            var goal = await _goalRepository.GetByIdAsync(id);

            if (goal == null || goal.UserId != userId)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                goal.Title = request.Title;
            }

            if (request.Description != null)
            {
                goal.Description = request.Description;
            }

            if (request.TargetAmount.HasValue)
            {
                goal.TargetAmount = request.TargetAmount.Value;
            }

            if (request.CurrentAmount.HasValue)
            {
                goal.CurrentAmount = request.CurrentAmount.Value;
            }

            if (request.TargetDate.HasValue)
            {
                goal.TargetDate = request.TargetDate;
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                goal.Status = request.Status;
            }

            if (!string.IsNullOrWhiteSpace(request.Currency))
            {
                goal.Currency = request.Currency;
            }

            if (!string.IsNullOrWhiteSpace(request.Icon))
            {
                goal.Icon = request.Icon;
            }

            if (!string.IsNullOrWhiteSpace(request.Color))
            {
                goal.Color = request.Color;
            }

            if (request.IsActive.HasValue)
            {
                goal.IsActive = request.IsActive.Value;
            }

            var updatedGoal = await _goalRepository.UpdateAsync(goal);
            return MapToDto(updatedGoal);
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            var goal = await _goalRepository.GetByIdAsync(id);

            if (goal == null || goal.UserId != userId)
            {
                return false;
            }

            return await _goalRepository.DeleteAsync(id);
        }

        private GoalDto MapToDto(Goal goal)
        {
            var progressPercentage = goal.TargetAmount > 0
                ? Math.Round((goal.CurrentAmount / goal.TargetAmount) * 100, 2)
                : 0;

            return new GoalDto
            {
                Id = goal.Id,
                UserId = goal.UserId,
                Title = goal.Title,
                Description = goal.Description,
                TargetAmount = goal.TargetAmount,
                CurrentAmount = goal.CurrentAmount,
                TargetDate = goal.TargetDate,
                Status = goal.Status,
                Currency = goal.Currency,
                Icon = goal.Icon,
                Color = goal.Color,
                IsActive = goal.IsActive,
                CreatedAt = goal.CreatedAt,
                UpdatedAt = goal.UpdatedAt,
                ProgressPercentage = progressPercentage
            };
        }
    }
}
