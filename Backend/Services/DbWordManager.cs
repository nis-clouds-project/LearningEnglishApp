using Backend.Models;
using Backend.Models.Data;
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
            return new List<Word>();
        }
    }

    public async Task<Word?> GetRandomWordAsync(long userId, long? categoryId = null)
    {
        try
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            var query = _context.Words.AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(w => w.category_id == categoryId.Value);
            }

            query = query.Where(w => !user.learned_words.Contains(w.Id) && w.user_id == DbInitializer.SystemUserId);

            var wordsCount = await query.CountAsync();
            if (wordsCount == 0)
            {
                return null;
            }

            var randomSkip = _random.Next(wordsCount);
            return await query.Skip(randomSkip).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
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

            await _userManager.AddCustomWordToListAsync(userId, word.Id);

            return word;
        }
        catch (Exception ex)
        {
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
                return false;
            }

            var word = await _context.Words.FindAsync(wordId);
            if (word == null)
            {
                return false;
            }

            return await _userManager.AddLearnedWordAsync(userId, wordId);
        }
        catch (Exception ex)
        {
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
            return new List<Word>();
        }
    }

    public async Task<Word?> GetRandomCustomWordAsync(long userId)
    {
        try
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            var myWordsCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == "My Words");

            if (myWordsCategory == null)
            {
                return null;
            }

            var userWords = await _context.Words
                .Include(w => w.Category)
                .Where(w => w.category_id == myWordsCategory.Id && w.user_id == userId)
                .ToListAsync();

            if (!userWords.Any())
            {
                return null;
            }

            var availableWords = userWords
                .Where(w => !user.learned_words.Contains(w.Id))
                .ToList();

            if (!availableWords.Any())
            {
                return null;
            }

            var randomWord = availableWords[_random.Next(availableWords.Count)];
            return randomWord;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<Word?> FindByEnglishAsync(string english)
    {
        try
        {
            var lower = english.ToLower();
            return await _context.Words
                .FirstOrDefaultAsync(w => w.Text.ToLower() == lower);
        }
        catch
        {
            return null;
        }
    }

    public async Task<Word?> FindByRussianAsync(string russian)
    {
        try
        {
            var lower = russian.ToLower();
            return await _context.Words
                .FirstOrDefaultAsync(w => w.Translation.ToLower() == lower);
        }
        catch
        {
            return null;
        }
    }
}