namespace MainServer.Models
{
    public class Story
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public bool IsAudio { get; set; }
        public bool IsTranslate { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public List<StoryContent> Contents { get; set; }
    }
}
