namespace MainServer.Models
{
    public class User
    {
        public string Id { get; set; } // 구글 계정 연동
        public string DeviceId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public List<Story> Stories { get; set; }
        public List<UserTag> UserTags { get; set; }
        public List<Recommendation> Recommendations { get; set; }
    }
}
