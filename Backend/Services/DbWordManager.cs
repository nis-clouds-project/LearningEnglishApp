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

    public async Task<List<Word>> GetRandomWordsForGeneratingTextAsync(long userId, CategoryType? category = null)
    {
        var user = await _userManager.GetUserAsync(userId);

        // Получаем слова из словаря пользователя
        var userWords = await _context.Words
            .Where(w => user.LearnedWordIds.Contains(w.Id))
            .Where(w => category == null || w.Category == category)
            .ToListAsync();

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

    public async Task<Word> GetRandomWordForLearningAsync(long userId, CategoryType category)
    {
        var user = await _userManager.GetUserAsync(userId);

        var availableWords = await _context.Words
            .Where(w => w.Category == category &&
                       !user.LearnedWordIds.Contains(w.Id) &&
                       !user.ViewedWordsWordIds.Contains(w.Id))
            .OrderBy(w => w.LastShown)
            .ToListAsync();

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
        var word = await _context.Words
            .FirstOrDefaultAsync(w => w.Id == wordId);

        if (word == null)
        {
            throw new WordNotFoundException();
        }

        return word;
    }

    public async Task<Word> AddCustomWordAsync(long userId, Word word)
    {
        var user = await _userManager.GetUserAsync(userId);

        _context.Words.Add(word);
        await _context.SaveChangesAsync();

        user.LearnedWordIds.Add(word.Id);
        await _context.SaveChangesAsync();

        return word;
    }
} 