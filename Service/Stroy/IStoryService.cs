using MainServer.Models;

namespace MainServer.Service.Stroy
{
    public interface IStoryService
    {
        Task<Story> CreateStory(Story story);
        Task<Story> GetStoryById(int id);
        Task<List<Story>> GetUserStories(string userId);
        Task<StoryContent> AddContent(StoryContent content);
        Task DeleteStory(int id);
        Task<List<Story>> GetStoriesByTag(int tagId);
    }
}