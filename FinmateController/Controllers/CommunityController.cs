using BLL.DTOs.Request;
using BLL.Services;
using FinmateController.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinmateController.Controllers
{
    [ApiController]
    [Route("api/v1/community")]
    [Authorize(AuthenticationSchemes = "Clerk")]
    public class CommunityController : FinmateControllerBase
    {
        private readonly CommunityService _communityService;
        private readonly ILogger<CommunityController> _logger;

        public CommunityController(
            CommunityService communityService,
            UserService userService,
            ILogger<CommunityController> logger)
            : base(userService)
        {
            _communityService = communityService;
            _logger = logger;
        }

        [HttpGet("posts")]
        public async Task<IActionResult> GetPosts([FromQuery] string? filter = "featured", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var posts = await _communityService.GetPostsAsync(userId.Value, filter, page, pageSize);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                var body = ApiErrorHelper.Build500Response(ex, _logger, "GetPosts");
                return StatusCode(500, body);
            }
        }

        [HttpPost("posts")]
        public async Task<IActionResult> CreatePost([FromBody] CreateCommunityPostDto request)
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

                var post = await _communityService.CreatePostAsync(userId.Value, request);
                return Ok(post);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var body = ApiErrorHelper.Build500Response(ex, _logger, "CreatePost");
                return StatusCode(500, body);
            }
        }

        [HttpPost("posts/{id:guid}/like")]
        public async Task<IActionResult> ToggleLike(Guid id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var post = await _communityService.ToggleLikeAsync(id, userId.Value);
                return Ok(post);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var body = ApiErrorHelper.Build500Response(ex, _logger, "ToggleLike");
                return StatusCode(500, body);
            }
        }

        [HttpPost("posts/{id:guid}/bookmark")]
        public async Task<IActionResult> ToggleBookmark(Guid id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var post = await _communityService.ToggleBookmarkAsync(id, userId.Value);
                return Ok(post);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var body = ApiErrorHelper.Build500Response(ex, _logger, "ToggleBookmark");
                return StatusCode(500, body);
            }
        }

        [HttpGet("posts/{id:guid}/comments")]
        public async Task<IActionResult> GetComments(Guid id)
        {
            try
            {
                var comments = await _communityService.GetCommentsAsync(id);
                return Ok(comments);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var body = ApiErrorHelper.Build500Response(ex, _logger, "GetComments");
                return StatusCode(500, body);
            }
        }

        [HttpPost("posts/{id:guid}/comments")]
        public async Task<IActionResult> CreateComment(Guid id, [FromBody] CreateCommunityCommentDto request)
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

                var comment = await _communityService.AddCommentAsync(id, userId.Value, request);
                return Ok(comment);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var body = ApiErrorHelper.Build500Response(ex, _logger, "CreateComment");
                return StatusCode(500, body);
            }
        }

        [HttpPost("follow/{userId:guid}")]
        public async Task<IActionResult> Follow(Guid userId)
        {
            try
            {
                var currentUserId = await GetCurrentUserIdAsync();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                await _communityService.FollowAsync(currentUserId.Value, userId);
                return Ok(new { message = "Followed successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var body = ApiErrorHelper.Build500Response(ex, _logger, "Follow");
                return StatusCode(500, body);
            }
        }

        [HttpDelete("follow/{userId:guid}")]
        public async Task<IActionResult> Unfollow(Guid userId)
        {
            try
            {
                var currentUserId = await GetCurrentUserIdAsync();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                await _communityService.UnfollowAsync(currentUserId.Value, userId);
                return Ok(new { message = "Unfollowed successfully" });
            }
            catch (Exception ex)
            {
                var body = ApiErrorHelper.Build500Response(ex, _logger, "Unfollow");
                return StatusCode(500, body);
            }
        }
    }
}
