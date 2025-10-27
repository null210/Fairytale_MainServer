using Google;
using MainServer.Data;
using MainServer.Models;
using Microsoft.EntityFrameworkCore;

namespace MainServer.BackgroundServices
{
    public class RecommendationUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RecommendationUpdateService> _logger;

        public RecommendationUpdateService(
            IServiceProvider serviceProvider,
            ILogger<RecommendationUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Recommendation Update Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<MainServerDbContext>();

                        // 사용자별 추천 점수 재계산
                        await UpdateRecommendationScores(context);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating recommendations");
                }

                // 1시간마다 실행
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

            _logger.LogInformation("Recommendation Update Service stopped");
        }

        private async Task UpdateRecommendationScores(MainServerDbContext context)
        {
            try
            {
                var users = await context.Users.ToListAsync();

                foreach (var user in users)
                {
                    // 사용자의 스토리 히스토리 분석
                    var storyTags = await context.Stories
                        .Where(s => s.UserId == user.Id)
                        .SelectMany(s => context.UserTags.Where(ut => ut.UserId == user.Id))
                        .GroupBy(ut => ut.TagId)
                        .Select(g => new { TagId = g.Key, Count = g.Count() })
                        .ToListAsync();

                    foreach (var tag in storyTags)
                    {
                        var recommendation = await context.Recommendations
                            .FirstOrDefaultAsync(r => r.UserId == user.Id && r.TagId == tag.TagId);

                        if (recommendation != null)
                        {
                            recommendation.TagCount = tag.Count;
                            context.Recommendations.Update(recommendation);
                        }
                        else
                        {
                            context.Recommendations.Add(new Recommendation
                            {
                                UserId = user.Id,
                                TagId = tag.TagId,
                                TagCount = tag.Count
                            });
                        }
                    }
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Recommendation scores updated for {UserCount} users", users.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateRecommendationScores");
                throw;
            }
        }
    }
}