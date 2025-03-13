using Backend.Models;

namespace Backend.Services.Interfaces;

/// <summary>
/// Интерфейс для управления пользователями.
/// </summary>
public interface IUserManager
{
    /// <summary>
    /// Получает пользователя по его идентификатору.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Объект пользователя или null, если пользователь не найден.</returns>
    Task<User?> GetUserAsync(long userId);

    /// <summary>
    /// Создает нового пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Созданный объект пользователя.</returns>
    Task<User> CreateUserAsync(long userId);

    /// <summary>
    /// Добавляет нового пользователя
    /// </summary>
    Task<User> AddUserAsync(long userId);

    /// <summary>
    /// Проверяет существование пользователя
    /// </summary>
    Task<bool> IsUserExistsAsync(long userId);

    /// <summary>
    /// Добавляет слово в список изученных слов пользователя
    /// </summary>
    Task<bool> AddLearnedWordAsync(long userId, long wordId);

    /// <summary>
    /// Добавляет слово в список собственных слов пользователя
    /// </summary>
    Task<bool> AddMyWordAsync(long userId, long wordId);

    /// <summary>
    /// Удаляет слово из списка собственных слов пользователя
    /// </summary>
    Task<bool> RemoveMyWordAsync(long userId, long wordId);

    /// <summary>
    /// Проверяет существование пользователя
    /// </summary>
    Task<bool> UserExistsAsync(long userId);
    
    /// <summary>
    /// Получает пользователя по ID
    /// </summary>
    Task<User?> GetUserByIdAsync(long userId);
    
    /// <summary>
    /// Добавляет слово в список пользовательских слов
    /// </summary>
    Task<bool> AddCustomWordToListAsync(long userId, long wordId);
}