using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Backend.Data.Configurations;

namespace Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Word> Words { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Применяем конфигурации
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new WordConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());

        // Дополнительные настройки для таблиц many-to-many
        modelBuilder.Entity<User>()
            .HasMany(u => u.LearnedWords)
            .WithMany(w => w.LearnedByUsers)
            .UsingEntity(j => 
            {
                j.ToTable("user_learned_words");
                j.Property("LearnedWordsId").HasColumnName("word_id");
                j.Property("LearnedByUsersId").HasColumnName("user_id");
            });

        modelBuilder.Entity<User>()
            .HasMany(u => u.ViewedWords)
            .WithMany(w => w.ViewedByUsers)
            .UsingEntity(j => 
            {
                j.ToTable("user_viewed_words");
                j.Property("ViewedWordsId").HasColumnName("word_id");
                j.Property("ViewedByUsersId").HasColumnName("user_id");
            });

        // Глобальные настройки для всех таблиц
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Используем snake_case для имен таблиц
            var tableName = entityType.GetTableName();
            if (tableName != null)
            {
                entityType.SetTableName(tableName.ToLower());
            }

            // Используем snake_case для имен столбцов
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (columnName != null)
                {
                    property.SetColumnName(columnName.ToLower());
                }
            }
        }
    }
} 