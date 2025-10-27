namespace MainServer.Models
{
    public class StoryContent
    {
        public int Id { get; set; }
        public int StoryId { get; set; }
        public string ContentType { get; set; } // Model, FModel, MModel, Text, Audio, TranslatedText
        public string FilePath { get; set; } // Google Drive path
        public string Content { get; set; }
        public Story Story { get; set; }
    }
}
