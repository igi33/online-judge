using Microsoft.EntityFrameworkCore;
using OnlineJudgeApi.Entities;

namespace OnlineJudgeApi.Helpers
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        // Entities
        public DbSet<User> Users { get; set; }
    }
}
