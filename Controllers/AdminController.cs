using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MainServer.Data;
using MainServer.Service;
using MainServer.Service.GoogleDrive;

namespace MainServer.Controllers
    {
        [Authorize(Roles = "Admin")]
        [ApiController]
        [Route("api/[controller]")]
        public class AdminController : ControllerBase
        {
            private readonly MainServerDbContext _context;
            private readonly IGoogleDriveService _driveService;
            private readonly ILogger<AdminController> _logger;

            public AdminController(
                MainServerDbContext context,
                IGoogleDriveService driveService,
                ILogger<AdminController> logger)
            {
                _context = context;
                _driveService = driveService;
                _logger = logger;
            }

            [HttpGet("users")]
            public async Task<IActionResult> GetAllUsers()
            {
                try
                {
                    var users = await _context.Users
                        .Include(u => u.Stories)
                        .Select(u => new
                        {
                            u.Id,
                            u.Name,
                            u.Email,
                            u.CreatedAt,
                            u.LastLoginAt,
                            StoryCount = u.Stories.Count,
                            IsGoogleUser = u.Email != null,
                            IsDeviceUser = u.DeviceId != null
                        })
                        .OrderByDescending(u => u.LastLoginAt)
                        .ToListAsync();

                    return Ok(users);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching users");
                    return StatusCode(500, "Internal server error");
                }
            }

            [HttpGet("stats")]
            public async Task<IActionResult> GetStatistics()
            {
                try
                {
                    var stats = new
                    {
                        TotalUsers = await _context.Users.CountAsync(),
                        TotalStories = await _context.Stories.CountAsync(),
                        TotalAudioStories = await _context.Stories.Where(s => s.IsAudio).CountAsync(),
                        TotalTranslatedStories = await _context.Stories.Where(s => s.IsTranslate).CountAsync(),
                        ActiveUsersToday = await _context.Users
                            .Where(u => u.LastLoginAt.Date == DateTime.Today)
                            .CountAsync(),
                        ActiveUsersThisWeek = await _context.Users
                            .Where(u => u.LastLoginAt >= DateTime.Today.AddDays(-7))
                            .CountAsync(),
                        PopularTags = await _context.UserTags
                            .GroupBy(ut => ut.Tag.TagName)
                            .Select(g => new { Tag = g.Key, Count = g.Count() })
                            .OrderByDescending(x => x.Count)
                            .Take(5)
                            .ToListAsync(),
                        StoriesPerDay = await GetStoriesPerDay(),
                        ContentTypes = await GetContentTypeDistribution()
                    };

                    return Ok(stats);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching statistics");
                    return StatusCode(500, "Internal server error");
                }
            }

            [HttpGet("drive/files")]
            public async Task<IActionResult> ListDriveFiles([FromQuery] string folderId = null)
            {
                try
                {
                    var files = await _driveService.ListFiles(folderId);
                    return Ok(files);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error listing drive files");
                    return StatusCode(500, "Internal server error");
                }
            }

            [HttpDelete("story/{id}")]
            public async Task<IActionResult> DeleteStory(int id)
            {
                try
                {
                    var story = await _context.Stories
                        .Include(s => s.Contents)
                        .FirstOrDefaultAsync(s => s.Id == id);

                    if (story == null)
                        return NotFound();

                    // Google Drive에서 관련 파일 삭제
                    foreach (var content in story.Contents.Where(c => !string.IsNullOrEmpty(c.FilePath)))
                    {
                        try
                        {
                            await _driveService.DeleteFile(content.FilePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete file {FileId} from Google Drive", content.FilePath);
                        }
                    }

                    _context.Stories.Remove(story);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Story {StoryId} deleted by admin", id);
                    return NoContent();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting story {StoryId}", id);
                    return StatusCode(500, "Internal server error");
                }
            }

            [HttpGet("logs")]
            public async Task<IActionResult> GetSystemLogs([FromQuery] int days = 7)
            {
                // 실제 구현시 로그 저장소에서 로그를 가져옴
                // 예시 응답
                return Ok(new
                {
                    Message = "Log retrieval would be implemented with proper logging infrastructure",
                    RequestedDays = days
                });
            }

            private async Task<object> GetStoriesPerDay()
            {
                var endDate = DateTime.Today;
                var startDate = endDate.AddDays(-30);

                var stories = await _context.Stories
                    .Where(s => s.Date >= startDate)
                    .GroupBy(s => s.Date.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                return stories;
            }

            private async Task<object> GetContentTypeDistribution()
            {
                var distribution = await _context.StoryContents
                    .GroupBy(c => c.ContentType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToListAsync();

                return distribution;
            }
        }
    }