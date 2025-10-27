using Google;
using MainServer.Models;
using MainServer.Data;
using Microsoft.EntityFrameworkCore;
using FairyTaleExplorer.DTOs;


namespace MainServer.Service.Recommend
{
    public class RecommendationService : IRecommendationService
    {
        private readonly MainServerDbContext _context;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(MainServerDbContext context, ILogger<RecommendationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<RecommendationDto>> GetUserRecommendations(string userId)
        {
            try
            {
                // 사용자의 태그 기반 추천 로직
                var userTags = await _context.UserTags
                    .Where(ut => ut.UserId == userId)
                    .Select(ut => ut.TagId)
                    .ToListAsync();

                if (!userTags.Any())
                {
                    // 기본 추천 (인기 태그)
                    return await _context.Tags
                        .Select(t => new RecommendationDto
                        {
                            TagId = t.Id,
                            TagName = t.TagName,
                            RecommendationScore = 1
                        })
                        .Take(3)
                        .ToListAsync();
                }

                // 태그 카운트 기반 추천
                var recommendations = await _context.Recommendations
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.TagCount)
                    .Select(r => new RecommendationDto
                    {
                        TagId = r.TagId,
                        TagName = r.Tag.TagName,
                        RecommendationScore = r.TagCount
                    })
                    .ToListAsync();

                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations for user {UserId}", userId);
                return new List<RecommendationDto>();
            }
        }

        public async Task UpdateUserTags(string userId, List<int> tagIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 기존 태그 삭제
                var existingTags = await _context.UserTags
                    .Where(ut => ut.UserId == userId)
                    .ToListAsync();

                _context.UserTags.RemoveRange(existingTags);

                // 새 태그 추가
                foreach (var tagId in tagIds)
                {
                    _context.UserTags.Add(new UserTag
                    {
                        UserId = userId,
                        TagId = tagId
                    });

                    // 추천 카운트 업데이트
                    var recommendation = await _context.Recommendations
                        .FirstOrDefaultAsync(r => r.UserId == userId && r.TagId == tagId);

                    if (recommendation != null)
                    {
                        recommendation.TagCount++;
                    }
                    else
                    {
                        _context.Recommendations.Add(new Recommendation
                        {
                            UserId = userId,
                            TagId = tagId,
                            TagCount = 1
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated tags for user {UserId}: {Tags}", userId, string.Join(", ", tagIds));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating tags for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Tag>> GetAllTags()
        {
            return await _context.Tags.ToListAsync();
        }
    }
}
