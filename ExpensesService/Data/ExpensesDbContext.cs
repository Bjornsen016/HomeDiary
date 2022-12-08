using ExpensesService.Model;
using Microsoft.EntityFrameworkCore;

namespace ExpensesService.Data
{
    public class ExpensesDbContext : DbContext
    {
        public ExpensesDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Expanse>? Expenses { get; set; }
    }
}
