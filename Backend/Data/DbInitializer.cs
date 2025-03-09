using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public static class DbInitializer
{
    public static async Task Initialize(AppDbContext context)
    {
        // Убедимся, что база данных создана
        await context.Database.EnsureCreatedAsync();

        // Проверим, есть ли уже слова в базе данных
        if (await context.Words.AnyAsync())
        {
            return; // База данных уже заполнена
        }

        var words = new List<Word>
        {
            // Food category
            new Word { Id = 1, Text = "apple", Translation = "яблоко", Category = "Food", LastShown = DateTime.UtcNow },
            new Word { Id = 2, Text = "banana", Translation = "банан", Category = "Food", LastShown = DateTime.UtcNow },
            new Word { Id = 3, Text = "bread", Translation = "хлеб", Category = "Food", LastShown = DateTime.UtcNow },
            
            // Technology category
            new Word { Id = 4, Text = "computer", Translation = "компьютер", Category = "Technology", LastShown = DateTime.UtcNow },
            new Word { Id = 5, Text = "smartphone", Translation = "смартфон", Category = "Technology", LastShown = DateTime.UtcNow },
            new Word { Id = 6, Text = "internet", Translation = "интернет", Category = "Technology", LastShown = DateTime.UtcNow },
            
            // Business category
            new Word { Id = 7, Text = "meeting", Translation = "встреча", Category = "Business", LastShown = DateTime.UtcNow },
            new Word { Id = 8, Text = "contract", Translation = "контракт", Category = "Business", LastShown = DateTime.UtcNow },
            new Word { Id = 9, Text = "deadline", Translation = "срок", Category = "Business", LastShown = DateTime.UtcNow },
            
            // Travel category
            new Word { Id = 10, Text = "airport", Translation = "аэропорт", Category = "Travel", LastShown = DateTime.UtcNow },
            new Word { Id = 11, Text = "hotel", Translation = "отель", Category = "Travel", LastShown = DateTime.UtcNow },
            new Word { Id = 12, Text = "passport", Translation = "паспорт", Category = "Travel", LastShown = DateTime.UtcNow },
            
            // Health category
            new Word { Id = 13, Text = "doctor", Translation = "врач", Category = "Health", LastShown = DateTime.UtcNow },
            new Word { Id = 14, Text = "hospital", Translation = "больница", Category = "Health", LastShown = DateTime.UtcNow },
            new Word { Id = 15, Text = "medicine", Translation = "лекарство", Category = "Health", LastShown = DateTime.UtcNow }
        };

        await context.Words.AddRangeAsync(words);
        await context.SaveChangesAsync();
    }
} 