namespace FairyTaleExplorer.DTOs
{
    public class RecommendationDto
    {
        public int TagId { get; set; }
        public string TagName { get; set; }
        public int RecommendationScore { get; set; }
    }

    public class GenerateTextResponse
    {
        public string Text { get; set; }
    }

    public class EnrichStoryResponse
    {
        public string EnrichedStory { get; set; }
    }

    public class TranslateResponse
    {
        public string TranslatedText { get; set; }
    }

    public class StoryDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public bool IsAudio { get; set; }
        public bool IsTranslate { get; set; }
        public string UserId { get; set; }
        public List<StoryContentDto> Contents { get; set; }
    }

    public class StoryContentDto
    {
        public int Id { get; set; }
        public string ContentType { get; set; }
        public string FilePath { get; set; }
        public string Content { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public int StoryCount { get; set; }
    }

    public class LoginResponseDto
    {
        public string Token { get; set; }
        public UserDto User { get; set; }
    }

    public class ErrorResponse
    {
        public string Message { get; set; }
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}