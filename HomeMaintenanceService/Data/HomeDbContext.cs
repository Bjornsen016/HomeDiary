using HomeMaintenanceService.Model;
using Microsoft.EntityFrameworkCore;

namespace HomeMaintenanceService.Data
{
    public class HomeDbContext : DbContext
    {
        public HomeDbContext(DbContextOptions options) : base(options) { }

        public DbSet<HomeTask> HomeTasks { get; set; }
        public DbSet<Note> Notes { get; set; }

    }
}
