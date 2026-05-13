using InteriorAI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteriorAI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        public DbSet<DesignResult> DesignResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DesignResult>(entity =>
            {
                entity.HasOne(design => design.User)
                    .WithMany()
                    .HasForeignKey(design => design.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(design => design.UserId);
                entity.HasIndex(design => design.CreatedAt);
                entity.HasIndex(design => design.Status);
                entity.HasIndex(design => new { design.UserId, design.IsDeleted, design.CreatedAt });
            });
        }
    }
}
