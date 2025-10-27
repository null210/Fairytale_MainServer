using FairyTaleExplorer.DTOs;
using MainServer.Models;

namespace MainServer.Service.Recommend
{
    public interface IRecommendationService
    {
        Task<List<RecommendationDto>> GetUserRecommendations(string userId);
        Task UpdateUserTags(string userId, List<int> tagIds);
        Task<List<Tag>> GetAllTags();
    }
}
