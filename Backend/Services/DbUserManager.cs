using Backend.Data;
using Backend.Exceptions;
using Backend.Models;
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

    public async Task<User> GetUserAsync(long userId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new UserNotFoundException($"Пользователь с ID {userId} не найден.");
        }

        return user;
    }

    public async Task<User> AddUserAsync(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "Объект пользователя не может быть null.");
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> IsUserExistsAsync(long userId)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId);
    }
} 