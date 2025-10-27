using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MainServer.Service.Recommend;
using FairyTaleExplorer.DTOs;

namespace FairyTaleExplorer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecommendations()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var recommendations = await _recommendationService.GetUserRecommendations(userId);
            return Ok(recommendations);
        }

        [HttpPost("update-tags")]
        public async Task<IActionResult> UpdateUserTags([FromBody] UpdateTagsDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _recommendationService.UpdateUserTags(userId, dto.TagIds);
            return Ok(new { message = "Tags updated successfully" });
        }

        [HttpGet("tags")]
        public async Task<IActionResult> GetAllTags()
        {
            var tags = await _recommendationService.GetAllTags();
            return Ok(tags);
        }
    }
}