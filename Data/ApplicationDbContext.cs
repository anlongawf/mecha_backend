using Microsoft.EntityFrameworkCore;
using Mecha.Models;

namespace Mecha.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<StyleModel> Styles { get; set; }
        public DbSet<User> Users { get; set; }
        
        public DbSet<UserStyle> UserStyles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserStyle>(entity =>
            {
                entity.ToTable("user_styles");
                entity.HasKey(e => e.StyleId);
                entity.Property(e => e.Styles).HasColumnType("json"); // cột JSON
            });
        }
    }
}