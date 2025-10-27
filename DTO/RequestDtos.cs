namespace FairyTaleExplorer.DTOs
{
    public class GoogleLoginDto
    {
        public string IdToken { get; set; }
    }

    public class DeviceLoginDto
    {
        public string DeviceId { get; set; }
    }

    public class StoryGenerationDto
    {
        public string Prompt { get; set; }
        public string Title { get; set; }
        public bool GenerateAudio { get; set; }
        public List<int> TagIds { get; set; }
    }

    public class UpdateTagsDto
    {
        public List<int> TagIds { get; set; }
    }

    public class TranslateRequestDto
    {
        public int StoryId { get; set; }
        public string TargetLanguage { get; set; }
    }

    public class ShareStoryDto
    {
        public int StoryId { get; set; }
        public string TargetUserId { get; set; }
        public string Message { get; set; }
    }
}