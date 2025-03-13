using Backend.Data;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Services;

public class DbUserManager : IUserManager
{
    private readonly AppDbContext _context;
    private readonly ILogger<DbUserManager> _logger;

    public DbUserManager(AppDbContext context, ILogger<DbUserManager> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(long userId)
    {
        try
        {
            return await _context.Users.FindAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return null;
        }
    }

    public async Task<User> CreateUserAsync(long userId)
    {
        try
        {
            var user = new User(userId);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UserExistsAsync(long userId)
    {
        try
        {
            return await _context.Users.AnyAsync(u => u.Id == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} exists", userId);
            return false;
        }
    }

    public Task<User?> GetUserAsync(long userId) => GetUserByIdAsync(userId);

    public Task<User> AddUserAsync(long userId) => CreateUserAsync(userId);

    public Task<bool> IsUserExistsAsync(long userId) => UserExistsAsync(userId);

    public async Task<bool> AddLearnedWordAsync(long userId, long wordId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            if (!user.learned_words.Contains(wordId))
            {
                user.learned_words.Add(wordId);
                _context.Entry(user).Property(u => u.learned_words).IsModified = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Added word {WordId} to learned words for user {UserId}", wordId, userId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding word {WordId} to learned words for user {UserId}", wordId, userId);
            return false;
        }
    }

    public async Task<bool> AddMyWordAsync(long userId, long wordId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            if (!user.my_words.Contains(wordId))
            {
                user.my_words.Add(wordId);
                _context.Entry(user).Property(u => u.my_words).IsModified = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Added word {WordId} to my words for user {UserId}", wordId, userId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding word {WordId} to my words for user {UserId}", wordId, userId);
            return false;
        }
    }

    public async Task<bool> RemoveMyWordAsync(long userId, long wordId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            if (user.my_words.Contains(wordId))
            {
                user.my_words.Remove(wordId);
                _context.Entry(user).Property(u => u.my_words).IsModified = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Removed word {WordId} from my words for user {UserId}", wordId, userId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing word {WordId} from my words for user {UserId}", wordId, userId);
            return false;
        }
    }

    public async Task<bool> AddCustomWordToListAsync(long userId, long wordId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            if (!user.my_words.Contains(wordId))
            {
                user.my_words.Add(wordId);
                _context.Entry(user).Property(u => u.my_words).IsModified = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Added custom word {WordId} to list for user {UserId}", wordId, userId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding custom word {WordId} to list for user {UserId}", wordId, userId);
            return false;
        }
    }
} 