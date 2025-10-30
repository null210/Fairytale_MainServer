using MainServer.Data;
using MainServer.Hubs;
using MainServer.Models;
using MainServer.Service.AI;
using MainServer.Service.GoogleDrive;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Http;        //10-30 임강묵 추가

namespace MainServer.BackgroundServices
{
    public class StoryProcessingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StoryProcessingService> _logger;
        private readonly IHubContext<StoryHub> _hubContext;

        public StoryProcessingService(
            IServiceProvider serviceProvider,
            ILogger<StoryProcessingService> logger,
            IHubContext<StoryHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Story Processing Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<MainServerDbContext>();
                        var aiService = scope.ServiceProvider.GetRequiredService<IAIService>();
                        var driveService = scope.ServiceProvider.GetRequiredService<IGoogleDriveService>();

                        // 오디오 생성이 필요한 스토리 처리
                        var pendingAudioStories = await context.Stories
                            .Where(s => s.IsAudio && !s.Contents.Any(c => c.ContentType == "Audio"))
                            .Include(s => s.Contents)
                            .ToListAsync(stoppingToken);

                        foreach (var story in pendingAudioStories)
                        {
                            await ProcessAudioGeneration(story, aiService, driveService, context);
                        }

                        // 번역이 필요한 스토리 처리
                        var pendingTranslations = await context.Stories
                            .Where(s => s.IsTranslate && !s.Contents.Any(c => c.ContentType == "TranslatedText"))
                            .Include(s => s.Contents)
                            .ToListAsync(stoppingToken);

                        foreach (var story in pendingTranslations)
                        {
                            await ProcessTranslation(story, aiService, context);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in story processing service");
                }

                // 30초마다 체크
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }

            _logger.LogInformation("Story Processing Service stopped");
        }

        private async Task ProcessAudioGeneration(Story story, IAIService aiService, IGoogleDriveService driveService, MainServerDbContext context)
        {
            try
            {
                _logger.LogInformation("Processing audio for story {StoryId}", story.Id);

                var textContent = story.Contents.FirstOrDefault(c => c.ContentType == "Text");
                if (textContent == null)
                {
                    _logger.LogWarning("No text content found for story {StoryId}", story.Id);
                    return;
                }

                //10-30 임강묵 추가*****************************************************************************
                var user = await context.Users.FirstOrDefaultAsync(u => u.Id == story.UserId);
                if (user == null || string.IsNullOrEmpty(user.ReferenceVoiceFileId))
                {
                    _logger.LogWarning("Story {StoryId}의 사용자({UserId})에게 등록된 참조 음성이 없습니다.", story.Id, story.UserId);
                    // 여기에 사용자에게 오류를 알리는 SignalR 로직을 추가할 수 있습니다.
                    return;
                }
                
                // 1. Google Drive에서 참조 음성 파일 다운로드
                var referenceAudioData = await driveService.DownloadFile(user.ReferenceVoiceFileId);
                if (referenceAudioData == null)
                {
                    _logger.LogError("참조 음성 파일 다운로드 실패: FileId={FileId}", user.ReferenceVoiceFileId);
                    return;
                }
    
                // 2. 다운로드한 byte[]를 IFormFile 객체로 변환
                using var stream = new MemoryStream(referenceAudioData);
                var referenceAudioFile = new FormFile(stream, 0, referenceAudioData.Length, "reference", "reference.wav")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "audio/wav" // 실제 파일 타입에 맞게 설정
                };
                //10-30 임강묵 추가*****************************************************************************
                
                // 진행 상황 알림 - 시작
                await _hubContext.Clients.Group($"user_{story.UserId}")
                    .SendAsync("AudioProgress", new { storyId = story.Id, progress = 10, status = "starting" });

                // TTS 생성
                var audioData = await aiService.GenerateAudio(textContent.Content);

                await _hubContext.Clients.Group($"user_{story.UserId}")
                    .SendAsync("AudioProgress", new { storyId = story.Id, progress = 60, status = "generating" });

                // Google Drive 업로드
                var fileName = $"audio_{story.Id}_{DateTime.Now:yyyyMMddHHmmss}.mp3";
                var fileId = await driveService.UploadFile(audioData, fileName);

                await _hubContext.Clients.Group($"user_{story.UserId}")
                    .SendAsync("AudioProgress", new { storyId = story.Id, progress = 90, status = "uploading" });

                // DB 저장
                var audioContent = new StoryContent
                {
                    StoryId = story.Id,
                    ContentType = "Audio",
                    FilePath = fileId,
                    Content = fileName
                };

                context.StoryContents.Add(audioContent);
                await context.SaveChangesAsync();

                // 완료 알림
                await _hubContext.Clients.Group($"user_{story.UserId}")
                    .SendAsync("AudioProgress", new
                    {
                        storyId = story.Id,
                        progress = 100,
                        status = "completed",
                        audioPath = fileId
                    });

                _logger.LogInformation("Audio generation completed for story {StoryId}", story.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating audio for story {StoryId}", story.Id);

                await _hubContext.Clients.Group($"user_{story.UserId}")
                    .SendAsync("AudioError", new
                    {
                        storyId = story.Id,
                        error = "Audio generation failed",
                        message = ex.Message
                    });
            }
        }

        private async Task ProcessTranslation(Story story, IAIService aiService, MainServerDbContext context)
        {
            try
            {
                _logger.LogInformation("Processing translation for story {StoryId}", story.Id);

                var textContent = story.Contents.FirstOrDefault(c => c.ContentType == "Text");
                if (textContent == null)
                {
                    _logger.LogWarning("No text content found for story {StoryId}", story.Id);
                    return;
                }

                // 번역 시작 알림
                await _hubContext.Clients.Group($"user_{story.UserId}")
                    .SendAsync("TranslationProgress", new
                    {
                        storyId = story.Id,
                        status = "translating"
                    });

                // 번역 수행
                var translatedText = await aiService.TranslateText(textContent.Content);

                // DB 저장
                var translatedContent = new StoryContent
                {
                    StoryId = story.Id,
                    ContentType = "TranslatedText",
                    Content = translatedText
                };

                context.StoryContents.Add(translatedContent);
                await context.SaveChangesAsync();

                // 완료 알림
                await _hubContext.Clients.Group($"user_{story.UserId}")
                    .SendAsync("TranslationComplete", new
                    {
                        storyId = story.Id,
                        translatedText = translatedText
                    });

                _logger.LogInformation("Translation completed for story {StoryId}", story.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating story {StoryId}", story.Id);

                await _hubContext.Clients.Group($"user_{story.UserId}")
                    .SendAsync("TranslationError", new
                    {
                        storyId = story.Id,
                        error = "Translation failed",
                        message = ex.Message
                    });
            }
        }
    }
}
