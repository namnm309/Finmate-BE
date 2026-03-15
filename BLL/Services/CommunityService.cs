using System.Text.RegularExpressions;
using BLL.DTOs.Request;
using BLL.DTOs.Response;
using DAL.Data;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class CommunityService
    {
        private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            "tips",
            "experience",
            "qa",
            "challenge"
        };

        private readonly FinmateContext _context;
        private readonly ILogger<CommunityService> _logger;

        public CommunityService(FinmateContext context, ILogger<CommunityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CommunityPostDto> CreatePostAsync(Guid userId, CreateCommunityPostDto request)
        {
            var normalizedCategory = NormalizeCategory(request.Category);
            var sanitizedContent = SanitizeContent(request.Content, 2000);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            var post = new CommunityPost
            {
                UserId = userId,
                Category = normalizedCategory,
                Content = sanitizedContent,
                LikesCount = 0,
                CommentsCount = 0,
                SharesCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CommunityPosts.Add(post);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[Community] Created post {PostId} by user {UserId}", post.Id, userId);

            return await GetPostOrThrowAsync(post.Id, userId);
        }

        public async Task<PagedResultDto<CommunityPostDto>> GetPostsAsync(Guid currentUserId, string? filter, int page, int pageSize)
        {
            var normalizedFilter = NormalizeFilter(filter);
            var safePage = Math.Max(1, page);
            var safePageSize = Math.Clamp(pageSize, 1, 50);

            var baseQuery = _context.CommunityPosts
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Bookmarks)
                .Include(p => p.Comments)
                .AsQueryable();

            IQueryable<CommunityPost> filteredQuery = normalizedFilter switch
            {
                "featured" => baseQuery
                    .OrderByDescending(p => p.LikesCount)
                    .ThenByDescending(p => p.CommentsCount)
                    .ThenByDescending(p => p.CreatedAt),
                "following" => baseQuery
                    .Where(p => p.UserId == currentUserId
                        || _context.UserFollows.Any(f =>
                            f.FollowerId == currentUserId && f.FollowingId == p.UserId))
                    .OrderByDescending(p => p.CreatedAt),
                _ => baseQuery.OrderByDescending(p => p.CreatedAt)
            };

            var total = await filteredQuery.CountAsync();
            var items = await filteredQuery
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToListAsync();

            var authorIds = items.Select(p => p.UserId).Distinct().ToList();
            var followedIds = await _context.UserFollows
                .AsNoTracking()
                .Where(f => f.FollowerId == currentUserId && authorIds.Contains(f.FollowingId))
                .Select(f => f.FollowingId)
                .ToListAsync();
            var followedSet = new HashSet<Guid>(followedIds);

            return new PagedResultDto<CommunityPostDto>
            {
                Items = items.Select(post => MapPost(post, currentUserId, followedSet)).ToList(),
                Total = total,
                Page = safePage,
                PageSize = safePageSize
            };
        }

        public async Task<CommunityPostDto> ToggleLikeAsync(Guid postId, Guid userId)
        {
            var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null)
            {
                throw new KeyNotFoundException("Post not found");
            }

            var existingLike = await _context.CommunityPostLikes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (existingLike == null)
            {
                _context.CommunityPostLikes.Add(new CommunityPostLike
                {
                    PostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                post.LikesCount += 1;
            }
            else
            {
                _context.CommunityPostLikes.Remove(existingLike);
                post.LikesCount = Math.Max(0, post.LikesCount - 1);
            }

            post.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetPostOrThrowAsync(postId, userId);
        }

        public async Task<CommunityPostDto> ToggleBookmarkAsync(Guid postId, Guid userId)
        {
            var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null)
            {
                throw new KeyNotFoundException("Post not found");
            }

            var existingBookmark = await _context.CommunityPostBookmarks
                .FirstOrDefaultAsync(b => b.PostId == postId && b.UserId == userId);

            if (existingBookmark == null)
            {
                _context.CommunityPostBookmarks.Add(new CommunityPostBookmark
                {
                    PostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                _context.CommunityPostBookmarks.Remove(existingBookmark);
            }

            post.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetPostOrThrowAsync(postId, userId);
        }

        public async Task<List<CommunityCommentDto>> GetCommentsAsync(Guid postId)
        {
            var postExists = await _context.CommunityPosts.AnyAsync(p => p.Id == postId);
            if (!postExists)
            {
                throw new KeyNotFoundException("Post not found");
            }

            var comments = await _context.CommunityPostComments
                .AsNoTracking()
                .Where(c => c.PostId == postId)
                .Include(c => c.User)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            return comments.Select(MapComment).ToList();
        }

        public async Task<CommunityCommentDto> AddCommentAsync(Guid postId, Guid userId, CreateCommunityCommentDto request)
        {
            var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null)
            {
                throw new KeyNotFoundException("Post not found");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            if (request.ParentCommentId.HasValue)
            {
                var parentExists = await _context.CommunityPostComments.AnyAsync(c =>
                    c.Id == request.ParentCommentId.Value && c.PostId == postId);

                if (!parentExists)
                {
                    throw new ArgumentException("Parent comment not found");
                }
            }

            var comment = new CommunityPostComment
            {
                PostId = postId,
                UserId = userId,
                ParentCommentId = request.ParentCommentId,
                Content = SanitizeContent(request.Content, 1000),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CommunityPostComments.Add(comment);
            post.CommentsCount += 1;
            post.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapComment(await _context.CommunityPostComments
                .AsNoTracking()
                .Include(c => c.User)
                .FirstAsync(c => c.Id == comment.Id));
        }

        public async Task FollowAsync(Guid followerId, Guid followingId)
        {
            if (followerId == followingId)
            {
                throw new ArgumentException("Cannot follow yourself");
            }

            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == followingId && u.IsActive);
            if (targetUser == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            var existing = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

            if (existing != null)
            {
                return;
            }

            _context.UserFollows.Add(new UserFollow
            {
                FollowerId = followerId,
                FollowingId = followingId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            _logger.LogInformation("[Community] User {FollowerId} followed {FollowingId}", followerId, followingId);
        }

        public async Task UnfollowAsync(Guid followerId, Guid followingId)
        {
            var existing = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

            if (existing != null)
            {
                _context.UserFollows.Remove(existing);
                await _context.SaveChangesAsync();
                _logger.LogInformation("[Community] User {FollowerId} unfollowed {FollowingId}", followerId, followingId);
            }
        }

        private async Task<CommunityPostDto> GetPostOrThrowAsync(Guid postId, Guid currentUserId)
        {
            var post = await _context.CommunityPosts
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Bookmarks)
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                throw new KeyNotFoundException("Post not found");
            }

            var followedSet = await _context.UserFollows
                .AsNoTracking()
                .Where(f => f.FollowerId == currentUserId && f.FollowingId == post.UserId)
                .AnyAsync()
                ? new HashSet<Guid> { post.UserId }
                : new HashSet<Guid>();

            return MapPost(post, currentUserId, followedSet);
        }

        private static CommunityPostDto MapPost(CommunityPost post, Guid currentUserId, HashSet<Guid> followedUserIds)
        {
            return new CommunityPostDto
            {
                Id = post.Id,
                Category = post.Category,
                Content = post.Content,
                LikesCount = post.LikesCount,
                CommentsCount = post.CommentsCount,
                SharesCount = post.SharesCount,
                LikedByCurrentUser = post.Likes.Any(l => l.UserId == currentUserId),
                BookmarkedByCurrentUser = post.Bookmarks.Any(b => b.UserId == currentUserId),
                IsFollowingByCurrentUser = post.UserId != currentUserId && followedUserIds.Contains(post.UserId),
                IsOwnPost = post.UserId == currentUserId,
                CreatedAt = post.CreatedAt,
                User = new CommunityAuthorDto
                {
                    Id = post.User.Id,
                    Name = post.User.FullName,
                    Initials = BuildInitials(post.User.FullName),
                    AvatarUrl = string.IsNullOrWhiteSpace(post.User.AvatarUrl) ? null : post.User.AvatarUrl,
                    Verified = post.User.IsPremium
                }
            };
        }

        private static CommunityCommentDto MapComment(CommunityPostComment comment)
        {
            return new CommunityCommentDto
            {
                Id = comment.Id,
                PostId = comment.PostId,
                Content = comment.Content,
                ParentCommentId = comment.ParentCommentId,
                CreatedAt = comment.CreatedAt,
                User = new CommunityAuthorDto
                {
                    Id = comment.User.Id,
                    Name = comment.User.FullName,
                    Initials = BuildInitials(comment.User.FullName),
                    AvatarUrl = string.IsNullOrWhiteSpace(comment.User.AvatarUrl) ? null : comment.User.AvatarUrl,
                    Verified = comment.User.IsPremium
                }
            };
        }

        private static string NormalizeFilter(string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return "featured";
            }

            return filter.Trim().ToLowerInvariant() switch
            {
                "newest" => "newest",
                "following" => "following",
                _ => "featured"
            };
        }

        private static string NormalizeCategory(string category)
        {
            var normalized = category?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!AllowedCategories.Contains(normalized))
            {
                throw new ArgumentException("Category is invalid");
            }

            return normalized;
        }

        private static string SanitizeContent(string content, int maxLength)
        {
            var trimmed = content?.Trim() ?? string.Empty;
            var withoutHtml = Regex.Replace(trimmed, "<.*?>", string.Empty);
            if (string.IsNullOrWhiteSpace(withoutHtml))
            {
                throw new ArgumentException("Content is required");
            }

            if (withoutHtml.Length > maxLength)
            {
                throw new ArgumentException($"Content must be {maxLength} characters or fewer");
            }

            return withoutHtml;
        }

        private static string BuildInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "U";
            }

            var parts = fullName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(part => char.ToUpperInvariant(part[0]));

            return string.Concat(parts);
        }
    }
}
