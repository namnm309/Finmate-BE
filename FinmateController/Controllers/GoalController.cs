using BLL.DTOs.Request;
using BLL.DTOs.Response;
using BLL.Services;
using FinmateController.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/goals")]
    [Authorize(AuthenticationSchemes = "Clerk")]
    public class GoalController : FinmateControllerBase
    {
        private readonly GoalService _goalService;
        private readonly ILogger<GoalController> _logger;

        public GoalController(
            GoalService goalService,
            UserService userService,
            ILogger<GoalController> logger)
            : base(userService)
        {
            _goalService = goalService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    _logger.LogWarning("[Goals] GetAll: User not authenticated - GetCurrentUserIdAsync returned null");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var goals = await _goalService.GetAllByUserIdAsync(userId.Value);
                _logger.LogInformation("[Goals] GetAll: UserId={UserId}, Count={Count}", userId.Value, goals.Count);
                return Ok(goals);
            }
            catch (Exception ex)
            {
                var body = ApiErrorHelper.Build500Response(ex, _logger, "GetAll");
                return StatusCode(500, body);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var goal = await _goalService.GetByIdAsync(id, userId.Value);
                if (goal == null)
                {
                    return NotFound(new { message = "Goal not found" });
                }

                return Ok(goal);
            }
            catch (Exception ex)
            {
                var body = ApiErrorHelper.Build500Response(ex, _logger, "GetById");
                return StatusCode(500, body);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateGoalDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var goal = await _goalService.CreateAsync(userId.Value, request);
                return CreatedAtAction(nameof(GetById), new { id = goal.Id }, goal);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var body = ApiErrorHelper.Build500Response(ex, _logger, "Create");
                return StatusCode(500, body);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGoalDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var goal = await _goalService.UpdateAsync(id, userId.Value, request);
                if (goal == null)
                {
                    return NotFound(new { message = "Goal not found" });
                }

                return Ok(goal);
            }
            catch (Exception ex)
            {
                var body = ApiErrorHelper.Build500Response(ex, _logger, "Update");
                return StatusCode(500, body);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var deleted = await _goalService.DeleteAsync(id, userId.Value);
                if (!deleted)
                {
                    return NotFound(new { message = "Goal not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                var body = ApiErrorHelper.Build500Response(ex, _logger, "Delete");
                return StatusCode(500, body);
            }
        }
    }
}
