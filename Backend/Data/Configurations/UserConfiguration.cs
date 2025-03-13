using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace Backend.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");
            
        // Конфигурация JSON полей для списков идентификаторов слов
        builder.Property(u => u.learned_words)
            .HasColumnName("learned_words")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<long>>(v) ?? new List<long>());
            
        builder.Property(u => u.my_words)
            .HasColumnName("my_words")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<long>>(v) ?? new List<long>());

        builder.Property(u => u.UserAiUsage)
            .HasColumnName("user_ai_usage")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<Dictionary<DateTime, int>>(v) ?? new Dictionary<DateTime, int>());

        // Конфигурация отношений
        builder.HasMany<Word>(u => u.LearnedWords)
            .WithMany(w => w.LearnedByUsers)
            .UsingEntity(j => j.ToTable("user_learned_words"));

        builder.HasMany<Word>(u => u.ViewedWords)
            .WithMany(w => w.ViewedByUsers)
            .UsingEntity(j => j.ToTable("user_viewed_words"));

        builder.HasMany<Word>(u => u.CustomWords)
            .WithOne()
            .HasForeignKey(w => w.user_id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}