using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Data;

public static class DbInitializer
{
    public const long SystemUserId = 1; 
    
    public static async Task Initialize(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

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
        
        var hasCategories = await context.Categories.AnyAsync();
        Console.WriteLine($"Has existing categories: {hasCategories}");

        if (!hasCategories)
        {
            Console.WriteLine("Adding categories...");
            // Получаем уникальные категории из InitialWords
            var uniqueCategories = InitialWords
                .Select(w => w.category)
                .Distinct()
                .Select(name => new Category { Name = name })
                .ToList();

            // Добавляем специальные категории, если их нет
            var specialCategories = new[]
            {
                "My Words",
                "Common Words",
                "Business",
                "Technology",
                "Education",
                "Science",
                "Arts",
                "Sports",
                "Health",
                "Food"
            };

            foreach (var categoryName in specialCategories)
            {
                if (!uniqueCategories.Any(c => c.Name == categoryName))
                {
                    uniqueCategories.Add(new Category { Name = categoryName });
                }
            }

            await context.Categories.AddRangeAsync(uniqueCategories);
            await context.SaveChangesAsync();
            Console.WriteLine($"Added {uniqueCategories.Count} categories");
        }

        var hasWords = await context.Words.AnyAsync();
        Console.WriteLine($"Has existing words: {hasWords}");

        if (!hasWords)
        {
            Console.WriteLine("Adding words...");
            var categories = await context.Categories.ToListAsync();
            Console.WriteLine($"Found {categories.Count} categories for word initialization");
            
            var words = new List<Word>();
            
            foreach (var (text, translation, categoryName) in InitialWords)
            {
                var category = categories.FirstOrDefault(c => c.Name == categoryName);
                if (category != null)
                {
                    words.Add(CreateWord(text, translation, category.Id));
                }
            }

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

    private static readonly List<(string text, string translation, string category)> InitialWords = new()
    {
        // Common Words
        ("hello", "привет", "Common Words"),
        ("goodbye", "до свидания", "Common Words"),
        ("thank you", "спасибо", "Common Words"),
        ("please", "пожалуйста", "Common Words"),
        ("sorry", "извините", "Common Words"),
        ("yes", "да", "Common Words"),
        ("no", "нет", "Common Words"),
        ("good morning", "доброе утро", "Common Words"),
        ("good evening", "добрый вечер", "Common Words"),
        ("good night", "спокойной ночи", "Common Words"),
        ("today", "сегодня", "Common Words"),
        ("tomorrow", "завтра", "Common Words"),
        ("yesterday", "вчера", "Common Words"),
        ("now", "сейчас", "Common Words"),
        ("later", "позже", "Common Words"),
        
        // Business
        ("meeting", "встреча", "Business"),
        ("report", "отчет", "Business"),
        ("contract", "контракт", "Business"),
        ("deadline", "срок", "Business"),
        ("manager", "менеджер", "Business"),
        ("budget", "бюджет", "Business"),
        ("client", "клиент", "Business"),
        ("project", "проект", "Business"),
        ("strategy", "стратегия", "Business"),
        ("investment", "инвестиция", "Business"),
        ("profit", "прибыль", "Business"),
        ("market", "рынок", "Business"),
        ("company", "компания", "Business"),
        ("office", "офис", "Business"),
        ("salary", "зарплата", "Business"),
        
        // Technology
        ("computer", "компьютер", "Technology"),
        ("phone", "телефон", "Technology"),
        ("internet", "интернет", "Technology"),
        ("email", "электронная почта", "Technology"),
        ("website", "веб-сайт", "Technology"),
        ("software", "программное обеспечение", "Technology"),
        ("hardware", "аппаратное обеспечение", "Technology"),
        ("network", "сеть", "Technology"),
        ("database", "база данных", "Technology"),
        ("application", "приложение", "Technology"),
        ("program", "программа", "Technology"),
        ("device", "устройство", "Technology"),
        ("screen", "экран", "Technology"),
        ("keyboard", "клавиатура", "Technology"),
        ("mouse", "мышь", "Technology"),
        
        // Education
        ("student", "студент", "Education"),
        ("teacher", "учитель", "Education"),
        ("school", "школа", "Education"),
        ("university", "университет", "Education"),
        ("lesson", "урок", "Education"),
        ("homework", "домашнее задание", "Education"),
        ("exam", "экзамен", "Education"),
        ("knowledge", "знание", "Education"),
        ("education", "образование", "Education"),
        ("study", "учиться", "Education"),
        ("book", "книга", "Education"),
        ("notebook", "тетрадь", "Education"),
        ("pen", "ручка", "Education"),
        ("pencil", "карандаш", "Education"),
        ("library", "библиотека", "Education"),
        
        // Science
        ("experiment", "эксперимент", "Science"),
        ("research", "исследование", "Science"),
        ("theory", "теория", "Science"),
        ("hypothesis", "гипотеза", "Science"),
        ("laboratory", "лаборатория", "Science"),
        ("scientist", "ученый", "Science"),
        ("discovery", "открытие", "Science"),
        ("chemistry", "химия", "Science"),
        ("physics", "физика", "Science"),
        ("biology", "биология", "Science"),
        ("mathematics", "математика", "Science"),
        ("equation", "уравнение", "Science"),
        ("molecule", "молекула", "Science"),
        ("atom", "атом", "Science"),
        ("energy", "энергия", "Science"),
        
        // Arts
        ("painting", "картина", "Arts"),
        ("music", "музыка", "Arts"),
        ("dance", "танец", "Arts"),
        ("theater", "театр", "Arts"),
        ("cinema", "кино", "Arts"),
        ("artist", "художник", "Arts"),
        ("musician", "музыкант", "Arts"),
        ("actor", "актер", "Arts"),
        ("sculpture", "скульптура", "Arts"),
        ("photography", "фотография", "Arts"),
        ("concert", "концерт", "Arts"),
        ("exhibition", "выставка", "Arts"),
        ("gallery", "галерея", "Arts"),
        ("performance", "выступление", "Arts"),
        ("creativity", "творчество", "Arts"),
        
        // Sports
        ("football", "футбол", "Sports"),
        ("basketball", "баскетбол", "Sports"),
        ("tennis", "теннис", "Sports"),
        ("volleyball", "волейбол", "Sports"),
        ("swimming", "плавание", "Sports"),
        ("running", "бег", "Sports"),
        ("cycling", "велоспорт", "Sports"),
        ("athlete", "спортсмен", "Sports"),
        ("competition", "соревнование", "Sports"),
        ("training", "тренировка", "Sports"),
        ("coach", "тренер", "Sports"),
        ("team", "команда", "Sports"),
        ("victory", "победа", "Sports"),
        ("medal", "медаль", "Sports"),
        ("championship", "чемпионат", "Sports"),
        
        // Health
        ("doctor", "врач", "Health"),
        ("hospital", "больница", "Health"),
        ("medicine", "лекарство", "Health"),
        ("health", "здоровье", "Health"),
        ("disease", "болезнь", "Health"),
        ("treatment", "лечение", "Health"),
        ("pharmacy", "аптека", "Health"),
        ("nurse", "медсестра", "Health"),
        ("patient", "пациент", "Health"),
        ("symptoms", "симптомы", "Health"),
        ("prescription", "рецепт", "Health"),
        ("vaccination", "вакцинация", "Health"),
        ("recovery", "выздоровление", "Health"),
        ("diet", "диета", "Health"),
        ("exercise", "упражнение", "Health"),
        
        // Food
        ("apple", "яблоко", "Food"),
        ("bread", "хлеб", "Food"),
        ("water", "вода", "Food"),
        ("coffee", "кофе", "Food"),
        ("tea", "чай", "Food"),
        ("milk", "молоко", "Food"),
        ("juice", "сок", "Food"),
        ("meat", "мясо", "Food"),
        ("fish", "рыба", "Food"),
        ("vegetable", "овощ", "Food"),
        ("fruit", "фрукт", "Food"),
        ("cheese", "сыр", "Food"),
        ("egg", "яйцо", "Food"),
        ("chicken", "курица", "Food"),
        ("rice", "рис", "Food")
    };
} 