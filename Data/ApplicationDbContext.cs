using Microsoft.EntityFrameworkCore;
using Mecha.Models;

namespace Mecha.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<StyleModel> Styles { get; set; }
        public DbSet<User> Users { get; set; }
    }
}