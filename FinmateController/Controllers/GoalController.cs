using BLL.DTOs.Request;
using BLL.DTOs.Response;
using BLL.Services;
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
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var goals = await _goalService.GetAllByUserIdAsync(userId.Value);
                return Ok(goals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting goals");
                return StatusCode(500, new { message = "Internal server error" });
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
                _logger.LogError(ex, "Error getting goal");
                return StatusCode(500, new { message = "Internal server error" });
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
                _logger.LogError(ex, "Error creating goal");
                return StatusCode(500, new { message = "Internal server error" });
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
                _logger.LogError(ex, "Error updating goal");
                return StatusCode(500, new { message = "Internal server error" });
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
                _logger.LogError(ex, "Error deleting goal");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
