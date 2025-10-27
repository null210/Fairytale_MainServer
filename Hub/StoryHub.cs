using MainServer.Service.AI;
using MainServer.Service.Stroy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MainServer.Hubs
{
    [Authorize]
    public class StoryHub : Hub
    {
        private readonly IStoryService _storyService;
        private readonly IAIService _aiService;
        private readonly ILogger<StoryHub> _logger;

        public StoryHub(IStoryService storyService, IAIService aiService, ILogger<StoryHub> logger)
        {
            _storyService = storyService;
            _aiService = aiService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            await base.OnConnectedAsync();

            _logger.LogInformation("User {UserId} connected with ConnectionId: {ConnectionId}",
                userId, Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.UserIdentifier;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            await base.OnDisconnectedAsync(exception);

            if (exception != null)
            {
                _logger.LogError(exception, "User {UserId} disconnected with error", userId);
            }
            else
            {
                _logger.LogInformation("User {UserId} disconnected", userId);
            }
        }

        //// 실시간 스토리 생성 스트리밍
        public async IAsyncEnumerable<string> StreamStoryGeneration(string prompt)
        {
            var userId = Context.UserIdentifier;
            _logger.LogInformation("Starting story generation for user {UserId}", userId);

            IAsyncEnumerable<string> aiStream = null;

            try
            {
                aiStream = StreamFromAI(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing AI stream for user {UserId}", userId);
                await Clients.Caller.SendAsync("GenerationError", new
                {
                    error = "Failed to initialize story generation",
                    timestamp = DateTime.UtcNow
                });
                yield break;
            }

            var hasError = false;
            await foreach (var chunk in aiStream)
            {
                yield return chunk;

                try
                {
                    // 클라이언트에 진행 상황 전송
                    await Clients.Caller.SendAsync("GenerationProgress", new
                    {
                        status = "generating",
                        progress = chunk.Length
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending progress for user {UserId}", userId);
                    hasError = true;
                    break;
                }
            }

            if (!hasError)
            {
                try
                {
                    await Clients.Caller.SendAsync("GenerationComplete", new
                    {
                        status = "completed",
                        timestamp = DateTime.UtcNow
                    });
                    _logger.LogInformation("Story generation completed for user {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending completion notification for user {UserId}", userId);
                }
            }
            else
            {
                await Clients.Caller.SendAsync("GenerationError", new
                {
                    error = "Story generation interrupted",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // 오디오 생성 진행 상황 알림
        public async Task NotifyAudioGenerationProgress(int storyId, int progress)
        {
            await Clients.Caller.SendAsync("AudioProgress", new
            {
                storyId = storyId,
                progress = progress,
                status = progress < 100 ? "processing" : "completed"
            });

            _logger.LogInformation("Audio generation progress for story {StoryId}: {Progress}%",
                storyId, progress);
        }

        // 다른 사용자와 스토리 공유
        public async Task ShareStory(int storyId, string targetUserId)
        {
            try
            {
                var story = await _storyService.GetStoryById(storyId);
                if (story != null)
                {
                    await Clients.Group($"user_{targetUserId}").SendAsync("StoryShared", new
                    {
                        storyId = storyId,
                        sharedBy = Context.UserIdentifier,
                        title = story.Title,
                        sharedAt = DateTime.UtcNow
                    });

                    _logger.LogInformation("Story {StoryId} shared from {FromUser} to {ToUser}",
                        storyId, Context.UserIdentifier, targetUserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing story {StoryId}", storyId);
                throw;
            }
        }

        // 실시간 번역 요청
        public async Task RequestTranslation(int storyId, string targetLanguage)
        {
            try
            {
                var story = await _storyService.GetStoryById(storyId);
                if (story != null)
                {
                    await Clients.Caller.SendAsync("TranslationStarted", new
                    {
                        storyId = storyId,
                        targetLanguage = targetLanguage
                    });

                    // 번역 처리는 백그라운드 서비스에서 수행
                    _logger.LogInformation("Translation requested for story {StoryId} to {Language}",
                        storyId, targetLanguage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting translation for story {StoryId}", storyId);
                throw;
            }
        }

        private async IAsyncEnumerable<string> StreamFromAI(string prompt)
        {
            // AI 서버와의 스트리밍 구현 (예시)
            var words = prompt.Split(' ');
            foreach (var word in words)
            {
                await Task.Delay(100); // 실제로는 AI 서버의 스트리밍 응답
                yield return word + " ";
            }
        }
    }
}
