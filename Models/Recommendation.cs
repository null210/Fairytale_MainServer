namespace MainServer.Models
{
    public class Recommendation
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int TagId { get; set; }
        public int TagCount { get; set; }
        public User User { get; set; }
        public Tag Tag { get; set; }
    }
}
