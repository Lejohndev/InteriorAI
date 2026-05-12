using InteriorAI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteriorAI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Bảng Users phải nằm TRONG cặp ngoặc nhọn của class AppDbContext
        public DbSet<User> Users { get; set; }
    }
}