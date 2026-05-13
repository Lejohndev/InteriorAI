﻿using System.Text.Json;
using InteriorAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteriorAI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<RoomInterior> RoomInteriors { get; set; } = default!;
        public DbSet<StyleAesthetic> StyleAesthetics { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình JsonSerializerOptions 
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // ==========================================
       
            
            modelBuilder.Entity<StyleAesthetic>(entity =>
            {
                entity.HasKey(e => e.StyleID);

                // LightingOptions
                entity.Property(e => e.LightingOptions)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonOptions),
                        v => JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>()); // Sử dụng ?? để đảm bảo không trả về null

                // MaterialOptions
                entity.Property(e => e.MaterialOptions)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonOptions),
                        v => JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>());

                // ColorRuleOptions
                entity.Property(e => e.ColorRuleOptions)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonOptions),
                        v => JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>());

                // AtmosphereOptions
                entity.Property(e => e.AtmosphereOptions)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonOptions),
                        v => JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>());
            });

            
            // 2. CẤU HÌNH BẢNG RoomInteriors
            
            modelBuilder.Entity<RoomInterior>(entity =>
            {
                entity.HasKey(e => e.RoomID);

                entity.HasOne(r => r.Style)
                      .WithMany(s => s.RoomInteriors)
                      .HasForeignKey(r => r.StyleID)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}