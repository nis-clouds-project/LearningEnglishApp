using Backend.Data;
using Backend.Exceptions;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Services;

public class DbWordManager : IWordManager
{
    private readonly AppDbContext _context;
    private readonly ILogger<DbWordManager> _logger;
    private readonly IUserManager _userManager;
    private readonly Random _random = new();

    public DbWordManager(AppDbContext context, ILogger<DbWordManager> logger, IUserManager userManager)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
    }

    /// <summary>
    /// Получает список изученных слов пользователя.
    /// </summary>
    public async Task<List<Word>> GetLearnedWordsAsync(long userId, long? categoryId = null)
    {
        try
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return new List<Word>();
            }

            var query = _context.Words.Where(w => user.learned_words.Contains(w.Id));

            if (categoryId.HasValue)
            {
                query = query.Where(w => w.category_id == categoryId.Value);
            }

            return await query.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting learned words for user {UserId}", userId);
            return new List<Word>();
        }
    }

    public async Task<List<Word>> GetRandomWordsForGeneratingTextAsync(long userId, long? categoryId = null)
    {
        try
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return new List<Word>();
            }

            var query = _context.Words.Where(w => user.learned_words.Contains(w.Id));

            if (categoryId.HasValue)
            {
                query = query.Where(w => w.category_id == categoryId.Value);
            }

            var words = await query.ToListAsync();
            return words.OrderBy(x => Guid.NewGuid()).Take(10).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting random words for text generation for user {UserId}", userId);
            return new List<Word>();
        }
    }

    public async Task<bool> AddWordToUserVocabularyAsync(long userId, long wordId)
    {
        var user = await _userManager.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new UserNotFoundException($"Пользователь с ID {userId} не найден");
        }

        var word = await GetWordByIdAsync(wordId);
        if (word == null)
        {
            throw new KeyNotFoundException($"Слово с ID {wordId} не найдено");
        }

        if (user.learned_words.Contains(wordId))
        {
            throw new WordAlreadyInVocabularyException();
        }

        user.learned_words.Add(wordId);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Word?> GetRandomWordAsync(long userId, long? categoryId = null)
    {
        try
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return null;
            }

            var query = _context.Words.AsQueryable();

            // Фильтруем по категории
            if (categoryId.HasValue)
            {
                query = query.Where(w => w.category_id == categoryId.Value);
            }

            // Исключаем изученные слова и берем только системные слова
            query = query.Where(w => !user.learned_words.Contains(w.Id) && w.user_id == DbInitializer.SystemUserId);

            // Получаем случайное слово
            var wordsCount = await query.CountAsync();
            if (wordsCount == 0)
            {
                _logger.LogWarning("No words available for learning for user {UserId} in category {CategoryId}", userId, categoryId);
                return null;
            }

            var randomSkip = _random.Next(wordsCount);
            return await query.Skip(randomSkip).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting random word for user {UserId}", userId);
            return null;
        }
    }

    public async Task<Word?> GetRandomWordForLearningAsync(long userId, long categoryId)
    {
        try
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return null;
            }

            var query = _context.Words
                .Where(w => w.category_id == categoryId)
                .Where(w => !user.learned_words.Contains(w.Id))
                .Where(w => w.user_id == DbInitializer.SystemUserId);

            var wordsCount = await query.CountAsync();
            if (wordsCount == 0)
            {
                _logger.LogWarning("No words available for learning for user {UserId} in category {CategoryId}", userId, categoryId);
                return null;
            }

            var randomSkip = _random.Next(wordsCount);
            return await query.Skip(randomSkip).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting random word for learning for user {UserId}", userId);
            return null;
        }
    }

    public async Task<Word?> GetWordByIdAsync(long wordId)
    {
        try
        {
            return await _context.Words.FindAsync(wordId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting word {WordId}", wordId);
            return null;
        }
    }

    public async Task<Word> AddCustomWordAsync(long userId, string text, string translation, long categoryId)
    {
        try
        {
            var word = new Word
            {
                Text = text,
                Translation = translation,
                category_id = categoryId,
                user_id = userId,
                IsCustom = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Words.Add(word);
            await _context.SaveChangesAsync();

            // Добавляем слово в список пользовательских слов
            await _userManager.AddCustomWordToListAsync(userId, word.Id);

            return word;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding custom word for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> AddWordToVocabularyAsync(long userId, long wordId)
    {
        try
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            var word = await _context.Words.FindAsync(wordId);
            if (word == null)
            {
                _logger.LogWarning("Word {WordId} not found", wordId);
                return false;
            }

            return await _userManager.AddLearnedWordAsync(userId, wordId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding word {WordId} to vocabulary for user {UserId}", wordId, userId);
            return false;
        }
    }

    public async Task<List<Word>> GetWordsByCategory(long categoryId)
    {
        try
        {
            return await _context.Words
                .Where(w => w.category_id == categoryId && w.user_id == DbInitializer.SystemUserId)
                .OrderBy(w => w.Text)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting words for category {CategoryId}", categoryId);
            return new List<Word>();
        }
    }

    public async Task<List<Word>> GetUserCustomWordsAsync(long userId)
    {
        try
        {
            return await _context.Words
                .Where(w => w.user_id == userId)
                .OrderBy(w => w.Text)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting custom words for user {UserId}", userId);
            return new List<Word>();
        }
    }

    public async Task<bool> DeleteCustomWordAsync(long userId, long wordId)
    {
        try
        {
            var word = await _context.Words.FindAsync(wordId);
            if (word == null || word.user_id != userId)
            {
                return false;
            }

            _context.Words.Remove(word);
            await _context.SaveChangesAsync();

            var user = await _userManager.GetUserByIdAsync(userId);
            if (user != null)
            {
                user.my_words.Remove(wordId);
                user.learned_words.Remove(wordId);
                _context.Entry(user).Property(u => u.my_words).IsModified = true;
                _context.Entry(user).Property(u => u.learned_words).IsModified = true;
                await _context.SaveChangesAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting custom word {WordId} for user {UserId}", wordId, userId);
            return false;
        }
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        try
        {
            return await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all categories");
            return new List<Category>();
        }
    }

    public async Task<List<Word>> GetAllWordsAsync()
    {
        try
        {
            return await _context.Words
                .Include(w => w.Category)
                .OrderBy(w => w.category_id)
                .ThenBy(w => w.Text)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all words");
            return new List<Word>();
        }
    }

    /// <inheritdoc />
    public async Task<Word?> GetRandomCustomWordAsync(long userId)
    {
        try
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return null;
            }

            var myWordsCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == "My Words");
            
            if (myWordsCategory == null)
            {
                _logger.LogError("My Words category not found");
                return null;
            }

            var userWords = await _context.Words
                .Include(w => w.Category)
                .Where(w => w.category_id == myWordsCategory.Id && w.user_id == userId)
                .ToListAsync();
            
            if (!userWords.Any())
            {
                _logger.LogInformation("No words found in My Words category for user {UserId}", userId);
                return null;
            }

            // Используем learned_words из объекта пользователя
            var availableWords = userWords
                .Where(w => !user.learned_words.Contains(w.Id))
                .ToList();

            if (!availableWords.Any())
            {
                _logger.LogInformation("All words in My Words category have been learned for user {UserId}", userId);
                return null;
            }

            var randomWord = availableWords[_random.Next(availableWords.Count)];
            return randomWord;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting random custom word for user {UserId}", userId);
            throw;
        }
    }
} 