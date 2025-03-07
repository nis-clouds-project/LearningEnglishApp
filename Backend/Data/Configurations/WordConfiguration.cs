using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations;

public class WordConfiguration : IEntityTypeConfiguration<Word>
{
    public void Configure(EntityTypeBuilder<Word> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .ValueGeneratedNever();
            
        builder.Property(x => x.Text)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(x => x.Translation)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(x => x.Category)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(x => x.LastShown)
            .IsRequired();
            
        // Создаем индексы для быстрого поиска
        builder.HasIndex(x => x.Text);
        builder.HasIndex(x => x.Translation);
        builder.HasIndex(x => x.Category);
    }
} 