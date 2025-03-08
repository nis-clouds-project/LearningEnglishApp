using Backend.Data;
using Backend.Exceptions;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class DbWordManager : IWordManager
{
    private readonly AppDbContext _context;
    private readonly IUserManager _userManager;
    private readonly Random _random = new();

    public DbWordManager(AppDbContext context, IUserManager userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<List<Word>> GetRandomWordsForGeneratingTextAsync(long userId, string? category = null)
    {
        var user = await _userManager.GetUserAsync(userId);

        // Получаем слова из словаря пользователя
        var query = _context.Words.AsQueryable();
        
        // Фильтруем по словам пользователя
        query = query.Where(w => user.LearnedWordIds.Contains(w.Id));
        
        // Фильтруем по категории, если она указана
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(w => w.Category == category);
        }

        var userWords = await query.ToListAsync();

        if (!userWords.Any())
        {
            throw new NoWordsAvailableException();
        }

        // Выбираем 10 случайных слов
        var selectedWords = userWords
            .OrderBy(x => x.LastShown)
            .Take(10)
            .ToList();

        // Обновляем время последнего показа
        foreach (var word in selectedWords)
        {
            word.LastShown = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();

        return selectedWords;
    }

    public async Task AddWordToUserVocabularyAsync(long userId, int wordId)
    {
        var user = await _userManager.GetUserAsync(userId);
        var word = await GetWordByIdAsync(wordId);

        if (user.LearnedWordIds.Contains(wordId))
        {
            throw new WordAlreadyInVocabularyException();
        }

        user.LearnedWordIds.Add(wordId);
        await _context.SaveChangesAsync();
    }

    public async Task<Word> GetRandomWordForLearningAsync(long userId, string category)
    {
        var user = await _userManager.GetUserAsync(userId);

        var query = _context.Words.AsQueryable();
        
        // Применяем фильтры
        query = query.Where(w => w.Category == category);
        query = query.Where(w => !user.LearnedWordIds.Contains(w.Id));
        query = query.Where(w => !user.ViewedWordsWordIds.Contains(w.Id));
        query = query.OrderBy(w => w.LastShown);

        var availableWords = await query.ToListAsync();

        if (!availableWords.Any())
        {
            throw new NoWordsAvailableException();
        }

        var word = availableWords.First();
        user.ViewedWordsWordIds.Add(word.Id);
        word.LastShown = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return word;
    }

    public async Task<Word> GetWordByIdAsync(int wordId)
    {
        var word = await _context.Words.FindAsync(wordId);
        if (word == null)
            throw new KeyNotFoundException($"Слово с ID {wordId} не найдено");
        return word;
    }

    public async Task<Word> AddCustomWordAsync(long userId, Word word)
    {
        var user = await _userManager.GetUserAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"Пользователь с ID {userId} не найден");

        _context.Words.Add(word);
        await _context.SaveChangesAsync();

        user.LearnedWordIds.Add(word.Id);
        await _context.SaveChangesAsync();

        return word;
    }

    public async Task<Word?> GetRandomWordAsync(User user, string? category = null)
    {
        var query = _context.Words.AsQueryable();

        // Исключаем выученные слова
        if (user.LearnedWordIds.Any())
        {
            query = query.Where(w => !user.LearnedWordIds.Contains(w.Id));
        }

        // Фильтруем по категории, если она указана
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(w => w.Category == category);
        }

        var words = await query.ToListAsync();
        
        if (!words.Any())
            return null;

        // Выбираем случайное слово
        var randomIndex = _random.Next(words.Count);
        var word = words[randomIndex];

        // Обновляем время последнего показа
        word.LastShown = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return word;
    }

    public async Task<bool> AddWordToVocabularyAsync(User user, int wordId)
    {
        var word = await _context.Words.FindAsync(wordId);
        if (word == null)
            return false;

        if (!user.LearnedWordIds.Contains(wordId))
        {
            user.LearnedWordIds.Add(wordId);
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<List<Word>> GetWordsByCategory(string category)
    {
        var query = _context.Words.AsQueryable();
        
        return await query
            .Where(w => w.Category == category)
            .OrderBy(w => w.Text)
            .ToListAsync();
    }
} 