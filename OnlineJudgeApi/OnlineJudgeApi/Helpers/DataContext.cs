using Microsoft.EntityFrameworkCore;
using OnlineJudgeApi.Entities;

namespace OnlineJudgeApi.Helpers
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        // Entities
        public DbSet<User> Users { get; set; }

        public DbSet<Task> Tasks { get; set; }
        
        public DbSet<TestCase> TestCases { get; set; }

        public DbSet<Tag> Tags { get; set; }

        public DbSet<TaskTag> TaskTags { get; set; }

        public DbSet<ComputerLanguage> ComputerLanguages { get; set; }

        public DbSet<Submission> Submissions { get; set; }

        // Composite keys can only be defined using Fluent API
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskTag>().HasKey(tt => new { tt.TaskId, tt.TagId });
        }
    }
}
