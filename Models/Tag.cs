namespace MainServer.Models
{
    public class Tag
    {

        public int Id { get; set; }
        public string? TagName { get; set; }
        public List<UserTag>? UserTags { get; set; }
        public List<Recommendation>? Recommendations { get; set; }
    }
}
