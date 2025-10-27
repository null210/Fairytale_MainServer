using Google;
using MainServer.Models;
using MainServer.Data;
using Microsoft.EntityFrameworkCore;

namespace MainServer.Service.Stroy
{
    public class StoryService : IStoryService
    {
        private readonly MainServerDbContext _context;
        private readonly ILogger<StoryService> _logger;

        public StoryService(MainServerDbContext context, ILogger<StoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Story> CreateStory(Story story)
        {
            try
            {
                _context.Stories.Add(story);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Story created: {story.Id}");
                return story;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating story");
                throw;
            }
        }

        public async Task<Story> GetStoryById(int id)
        {
            return await _context.Stories
                .Include(s => s.Contents)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<Story>> GetUserStories(string userId)
        {
            return await _context.Stories
                .Where(s => s.UserId == userId)
                .Include(s => s.Contents)
                .OrderByDescending(s => s.Date)
                .ToListAsync();
        }

        public async Task<StoryContent> AddContent(StoryContent content)
        {
            try
            {
                _context.StoryContents.Add(content);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Content added to story {content.StoryId}: {content.ContentType}");
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding content to story {content.StoryId}");
                throw;
            }
        }

        public async Task DeleteStory(int id)
        {
            var story = await _context.Stories
                .Include(s => s.Contents)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (story != null)
            {
                // 관련 콘텐츠도 함께 삭제
                _context.StoryContents.RemoveRange(story.Contents);
                _context.Stories.Remove(story);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Story deleted: {id}");
            }
        }

        public async Task<List<Story>> GetStoriesByTag(int tagId)
        {
            // 태그와 연관된 사용자들의 스토리 조회
            var userIds = await _context.UserTags
                .Where(ut => ut.TagId == tagId)
                .Select(ut => ut.UserId)
                .Distinct()
                .ToListAsync();

            return await _context.Stories
                .Where(s => userIds.Contains(s.UserId))
                .Include(s => s.Contents)
                .OrderByDescending(s => s.Date)
                .Take(10)
                .ToListAsync();
        }
    }
}
