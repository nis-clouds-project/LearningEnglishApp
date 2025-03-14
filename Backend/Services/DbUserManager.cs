using Backend.Models;
using Backend.Models.Data;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class DbUserManager : IUserManager
{
    private readonly AppDbContext _context;

    public DbUserManager(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByIdAsync(long userId)
    {
        try
        {
            return await _context.Users.FindAsync(userId);
        }
        catch (Exception ex)
        {
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
            return false;
        }
    }

    public async Task<bool> AddLearnedWordAsync(long userId, long wordId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            if (!user.learned_words.Contains(wordId))
            {
                user.learned_words.Add(wordId);
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

    public async Task<bool> AddCustomWordToListAsync(long userId, long wordId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            if (!user.my_words.Contains(wordId))
            {
                user.my_words.Add(wordId);
                _context.Entry(user).Property(u => u.my_words).IsModified = true;
                await _context.SaveChangesAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
} 