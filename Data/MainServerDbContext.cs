using Google;
using MainServer.Models;
using Microsoft.EntityFrameworkCore;
using static MainServer.Models.Tag;

namespace MainServer.Data
{
    public class MainServerDbContext : DbContext
    {
        public MainServerDbContext(DbContextOptions<MainServerDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Story> Stories { get; set; }
        public DbSet<StoryContent> StoryContents { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<UserTag> UserTags { get; set; }
        public DbSet<Recommendation> Recommendations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // UserTag composite key
            modelBuilder.Entity<UserTag>()
                .HasKey(ut => new { ut.UserId, ut.TagId });

            // Relationships
            modelBuilder.Entity<UserTag>()
                .HasOne(ut => ut.User)
                .WithMany(u => u.UserTags)
                .HasForeignKey(ut => ut.UserId);

            modelBuilder.Entity<UserTag>()
                .HasOne(ut => ut.Tag)
                .WithMany(t => t.UserTags)
                .HasForeignKey(ut => ut.TagId);

            // Seed initial tags
            modelBuilder.Entity<Tag>().HasData(
                new Tag { Id = 1, TagName = "우주" },
                new Tag { Id = 2, TagName = "공룡" },
                new Tag { Id = 3, TagName = "용사" }
            );
        }
    }
}
