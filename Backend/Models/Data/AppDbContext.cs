using Backend.Models.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Data;

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

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new WordConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());

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

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName != null)
            {
                entityType.SetTableName(tableName.ToLower());
            }

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