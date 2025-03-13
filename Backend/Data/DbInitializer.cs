using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Data;

public static class DbInitializer
{
    // Константы
    public const long SystemUserId = 1; // ID для системного пользователя
    
    public static async Task Initialize(AppDbContext context)
    {
        // Убедимся, что база данных создана
        await context.Database.EnsureCreatedAsync();

        // Создаем системного пользователя, если его нет
        if (!await context.Users.AnyAsync(u => u.Id == SystemUserId))
        {
            Console.WriteLine("Creating system user...");
            var systemUser = new User
            {
                Id = SystemUserId,
                learned_words = new List<long>(),
                my_words = new List<long>(),
                UserAiUsage = new Dictionary<DateTime, int>()
            };
            context.Users.Add(systemUser);
            await context.SaveChangesAsync();
            Console.WriteLine("System user created successfully");
        }
        
        // Проверяем, есть ли уже категории
        var hasCategories = await context.Categories.AnyAsync();
        Console.WriteLine($"Has existing categories: {hasCategories}");

        if (!hasCategories)
        {
            Console.WriteLine("Adding categories...");
            // Добавляем базовые категории
            var categories = new List<Category>
            {
                new Category { Name = "My Words" },
                new Category { Name = "Common Words" },
                new Category { Name = "Business" },
                new Category { Name = "Technology" },
                new Category { Name = "Travel" },
                new Category { Name = "Education" },
                new Category { Name = "Science" },
                new Category { Name = "Arts" },
                new Category { Name = "Sports" },
                new Category { Name = "Health" },
                new Category { Name = "Food" }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
            Console.WriteLine($"Added {categories.Count} categories");
        }

        // Получаем ID категории "My Words" (она должна быть первой)
        var myWordsCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "My Words");
        if (myWordsCategory == null)
        {
            throw new Exception("My Words category not found");
        }

        // Проверяем, есть ли уже слова
        var hasWords = await context.Words.AnyAsync();
        Console.WriteLine($"Has existing words: {hasWords}");

        if (!hasWords)
        {
            Console.WriteLine("Adding words...");
            var categories = await context.Categories.ToListAsync();
            Console.WriteLine($"Found {categories.Count} categories for word initialization");
            
            var commonWordsCategory = categories.First(c => c.Name == "Common Words");
            var businessCategory = categories.First(c => c.Name == "Business");
            var technologyCategory = categories.First(c => c.Name == "Technology");
            var travelCategory = categories.First(c => c.Name == "Travel");
            var educationCategory = categories.First(c => c.Name == "Education");

            var words = new List<Word>
            {
                // Common Words
                CreateWord("hello", "привет", commonWordsCategory.Id),
                CreateWord("goodbye", "до свидания", commonWordsCategory.Id),
                CreateWord("thank you", "спасибо", commonWordsCategory.Id),
                CreateWord("please", "пожалуйста", commonWordsCategory.Id),
                CreateWord("sorry", "извините", commonWordsCategory.Id),
                CreateWord("friend", "друг", commonWordsCategory.Id),
                CreateWord("family", "семья", commonWordsCategory.Id),
                CreateWord("love", "любовь", commonWordsCategory.Id),
                CreateWord("time", "время", commonWordsCategory.Id),
                CreateWord("day", "день", commonWordsCategory.Id),

                // Business
                CreateWord("meeting", "встреча", businessCategory.Id),
                CreateWord("report", "отчет", businessCategory.Id),
                CreateWord("contract", "контракт", businessCategory.Id),
                CreateWord("deadline", "срок", businessCategory.Id),
                CreateWord("manager", "менеджер", businessCategory.Id),
                CreateWord("budget", "бюджет", businessCategory.Id),
                CreateWord("client", "клиент", businessCategory.Id),
                CreateWord("project", "проект", businessCategory.Id),
                CreateWord("strategy", "стратегия", businessCategory.Id),
                CreateWord("investment", "инвестиция", businessCategory.Id),

                // Technology
                CreateWord("computer", "компьютер", technologyCategory.Id),
                CreateWord("internet", "интернет", technologyCategory.Id),
                CreateWord("software", "программное обеспечение", technologyCategory.Id),
                CreateWord("hardware", "аппаратное обеспечение", technologyCategory.Id),
                CreateWord("database", "база данных", technologyCategory.Id),
                CreateWord("algorithm", "алгоритм", technologyCategory.Id),
                CreateWord("network", "сеть", technologyCategory.Id),
                CreateWord("security", "безопасность", technologyCategory.Id),
                CreateWord("cloud", "облако", technologyCategory.Id),
                CreateWord("programming", "программирование", technologyCategory.Id),

                // Travel
                CreateWord("airport", "аэропорт", travelCategory.Id),
                CreateWord("hotel", "отель", travelCategory.Id),
                CreateWord("passport", "паспорт", travelCategory.Id),
                CreateWord("ticket", "билет", travelCategory.Id),
                CreateWord("luggage", "багаж", travelCategory.Id),
                CreateWord("vacation", "отпуск", travelCategory.Id),
                CreateWord("journey", "путешествие", travelCategory.Id),
                CreateWord("destination", "место назначения", travelCategory.Id),
                CreateWord("tourist", "турист", travelCategory.Id),
                CreateWord("guide", "гид", travelCategory.Id),

                // Education
                CreateWord("student", "студент", educationCategory.Id),
                CreateWord("teacher", "учитель", educationCategory.Id),
                CreateWord("school", "школа", educationCategory.Id),
                CreateWord("university", "университет", educationCategory.Id),
                CreateWord("lesson", "урок", educationCategory.Id),
                CreateWord("homework", "домашнее задание", educationCategory.Id),
                CreateWord("exam", "экзамен", educationCategory.Id),
                CreateWord("knowledge", "знание", educationCategory.Id),
                CreateWord("education", "образование", educationCategory.Id),
                CreateWord("study", "учиться", educationCategory.Id)
            };

            Console.WriteLine($"Preparing to add {words.Count} words");
            await context.Words.AddRangeAsync(words);
            await context.SaveChangesAsync();
            Console.WriteLine("Words added successfully");
        }
    }

    private static Word CreateWord(string text, string translation, long categoryId)
    {
        return new Word
        {
            Text = text,
            Translation = translation,
            category_id = categoryId,
            user_id = SystemUserId,
            IsCustom = false,
            CreatedAt = DateTime.UtcNow
        };
    }
} 