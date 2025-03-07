using LearningBotCore.exceptions;
using LearningBotCore.model;
using LearningBotCore.service.interfaces;

namespace LearningBotCore.service;

/// <summary>
/// Реализация интерфейса <see cref="IUserManager"/> для управления пользователями в памяти.
/// Хранит пользователей в словаре, где ключом является идентификатор пользователя (long), а значение — объект <see cref="User"/>.
/// </summary>
public class InMemoryUserManager : IUserManager
{
    // Словарь для хранения пользователей. Ключ — идентификатор пользователя (long), значение — объект User.
    private readonly Dictionary<long, User> _users = new();

    /// <summary>
    /// Добавляет пользователя в словарь.
    /// Если пользователь с таким идентификатором уже существует, он будет перезаписан.
    /// </summary>
    /// <param name="user">Объект пользователя для добавления.</param>
    /// <exception cref="ArgumentNullException">Если переданный объект пользователя равен null.</exception>
    public void AddUser(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "Объект пользователя не может быть null.");
        }

        _users[user.Id] = user;
    }

    /// <summary>
    /// Возвращает пользователя по его идентификатору.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Объект пользователя.</returns>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь с указанным идентификатором не найден.</exception>
    public User GetUser(long userId)
    {
        if (!IsExistUser(userId))
        {
            throw new UserNotFoundException($"Пользователь с ID {userId} не найден.");
        }

        return _users[userId];
    }

    /// <summary>
    /// Проверяет, существует ли пользователь с указанным идентификатором.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>True, если пользователь существует, иначе — False.</returns>
    private bool IsExistUser(long userId)
    {
        return _users.ContainsKey(userId);
    }
}