using MainServer.Models;
using MainServer.Service.AI;
using MainServer.Service.GoogleDrive;
using MainServer.Service.Stroy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MainServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StoryController : ControllerBase
    {
        private readonly IStoryService _storyService;
        private readonly IAIService _aiService;
        private readonly IGoogleDriveService _driveService;

        public StoryController(IStoryService storyService, IAIService aiService, IGoogleDriveService driveService)
        {
            _storyService = storyService;
            _aiService = aiService;
            _driveService = driveService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateStory([FromBody] FairyTaleExplorer.DTOs.StoryGenerationDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // AI 서버로 요청 (string, text 생성)
            var storyText = await _aiService.GenerateStoryText(dto.Prompt);

            // 생성형 LLM으로 이야기 생성
            var enrichedStory = await _aiService.EnrichStory(storyText);

            // TTS 생성 (선택적)
            string audioPath = null;
            if (dto.GenerateAudio)
            {
                var audioData = await _aiService.GenerateAudio(enrichedStory);
                audioPath = await _driveService.UploadFile(audioData, $"audio_{DateTime.Now.Ticks}.mp3");
            }

            // Story 저장
            var story = new Story
            {
                Title = dto.Title ?? $"이야기_{DateTime.Now:yyyyMMdd}",
                Date = DateTime.Now,
                IsAudio = dto.GenerateAudio,
                IsTranslate = false,
                UserId = userId
            };

            await _storyService.CreateStory(story);

            // 콘텐츠 저장
            var textContent = new StoryContent
            {
                StoryId = story.Id,
                ContentType = "Text",
                Content = enrichedStory
            };
            await _storyService.AddContent(textContent);

            return Ok(new { storyId = story.Id, story = enrichedStory, audioPath });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetUserStories()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var stories = await _storyService.GetUserStories(userId);
            return Ok(stories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStory(int id)
        {
            var story = await _storyService.GetStoryById(id);
            if (story == null)
                return NotFound();

            return Ok(story);
        }

        [HttpPost("{id}/translate")]
        public async Task<IActionResult> TranslateStory(int id)
        {
            var story = await _storyService.GetStoryById(id);
            if (story == null)
                return NotFound();

            var textContent = story.Contents.FirstOrDefault(c => c.ContentType == "Text");
            if (textContent == null)
                return BadRequest("No text content found");

            var translatedText = await _aiService.TranslateText(textContent.Content);

            var translatedContent = new StoryContent
            {
                StoryId = id,
                ContentType = "TranslatedText",
                Content = translatedText
            };

            await _storyService.AddContent(translatedContent);

            return Ok(new { translatedText });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStory(int id)
        {
            await _storyService.DeleteStory(id);
            return NoContent();
        }
    }
}