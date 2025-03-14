using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Models.Data.Configurations;

public class WordConfiguration : IEntityTypeConfiguration<Word>
{
    public void Configure(EntityTypeBuilder<Word> builder)
    {
        builder.ToTable("words");

        // Первичный ключ
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id)
            .HasColumnName("id");

        // Основные свойства
        builder.Property(w => w.Text)
            .HasColumnName("text")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(w => w.Translation)
            .HasColumnName("translation")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(w => w.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(w => w.IsCustom)
            .HasColumnName("is_custom")
            .IsRequired()
            .HasDefaultValue(false);

        // Внешние ключи
        builder.Property(w => w.user_id)
            .HasColumnName("user_id");

        builder.Property(w => w.category_id)
            .HasColumnName("category_id");

        // Отношения
        builder.HasOne<Category>(w => w.Category)
            .WithMany()
            .HasForeignKey(w => w.category_id)
            .OnDelete(DeleteBehavior.SetNull);

        // Отношения многие-ко-многим с User
        builder.HasMany<User>(w => w.LearnedByUsers)
            .WithMany(u => u.LearnedWords)
            .UsingEntity(j => j.ToTable("user_learned_words"));

        builder.HasMany<User>(w => w.ViewedByUsers)
            .WithMany(u => u.ViewedWords)
            .UsingEntity(j => j.ToTable("user_viewed_words"));

        // Индексы
        builder.HasIndex(w => w.Text)
            .HasDatabaseName("ix_words_text");

        builder.HasIndex(w => w.category_id)
            .HasDatabaseName("ix_words_category_id");

        builder.HasIndex(w => w.user_id)
            .HasDatabaseName("ix_words_user_id");
    }
} 