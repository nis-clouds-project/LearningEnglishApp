using Backend.Models;

namespace Backend.Services.Interfaces;

/// <summary>
/// Интерфейс для управления пользователями.
/// </summary>
public interface IUserManager
{
    /// <summary>
    /// Добавляет пользователя в систему.
    /// </summary>
    /// <param name="user">Объект пользователя для добавления.</param>
    /// <returns>Добавленный пользователь.</returns>
    Task<User> AddUserAsync(User user);

    /// <summary>
    /// Возвращает пользователя по его идентификатору.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Объект пользователя.</returns>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь с указанным идентификатором не найден.</exception>
    Task<User> GetUserAsync(long userId);

    /// <summary>
    /// Проверяет существование пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>True если пользователь существует, иначе false.</returns>
    Task<bool> IsUserExistsAsync(long userId);
}