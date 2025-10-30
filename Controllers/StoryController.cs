using MainServer.Models;
using MainServer.Service.AI;
using MainServer.Service.GoogleDrive;
using MainServer.Service.Stroy;
using MainServer.Service.User;
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
        private readonly IUserService _userService;        //10-30 임강묵 추가

        
        public StoryController(IStoryService storyService, IAIService aiService, IGoogleDriveService driveService, IUserService userService)
        {
            _storyService = storyService;
            _aiService = aiService;
            _driveService = driveService;
            _userService = userService;                    //10-30 임강묵 추가
        }

        //***********************************************************************10-30 임강묵 추가
        [HttpPost("register-voice")]
        public async Task<IActionResult> RegisterUserVoice(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                return BadRequest("오디오 파일이 필요합니다.");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userService.GetUserById(userId);

            if (user == null)
            {
                return Unauthorized();
            }

            // Google Drive에 참조 음성 파일 업로드
            // 스트림을 바이트 배열로 변환하는 헬퍼 메서드가 필요할 수 있습니다.
            using var memoryStream = new MemoryStream();
            await audioFile.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var fileName = $"ref_voice_{userId}_{DateTime.UtcNow.Ticks}.wav";
            var fileId = await _driveService.UploadFile(fileBytes, fileName);

            // 사용자의 ReferenceVoiceFileId 업데이트
            user.ReferenceVoiceFileId = fileId;
            await _userService.UpdateUser(user);

            return Ok(new { message = "대표 목소리가 성공적으로 등록되었습니다.", fileId = fileId });
        }   
         //******************************************************************************10-30 임강묵 추가
         
         //******************************************************************************10-30 임강묵 수정
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateStory([FromForm] FairyTaleExplorer.DTOs.StoryGenerationDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // AI 서버로 요청 (string, text 생성)
            var storyText = await _aiService.GenerateStoryText(dto.Prompt);

            // 생성형 LLM으로 이야기 생성
            var enrichedStory = await _aiService.EnrichStory(storyText);

            // TTS 생성 (선택적)
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 2. (선택적) 오디오 생성 요청 시, 사용자가 대표 목소리를 등록했는지 확인하는 방어 로직
            if (dto.GenerateAudio)
            {
                var user = await _userService.GetUserById(userId);
                // '대표 목소리' 정보가 User 모델에 추가되었다고 가정합니다. (예: user.RepresentativeVoiceId)
                if (string.IsNullOrEmpty(user?.ReferenceVoiceFileId)) // 실제 속성명에 맞게 변경 필요
                {
                    return BadRequest("오디오를 생성하려면 먼저 대표 목소리를 등록해야 합니다.");
                }
            }
            //******************************************************************************10-30 임강묵 수정

            
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

            return Ok(new { storyId = story.Id, story = enrichedStory});            //10-30 임강묵 수정
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
