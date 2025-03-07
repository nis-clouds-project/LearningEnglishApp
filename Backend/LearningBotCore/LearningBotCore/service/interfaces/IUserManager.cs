using LearningBotCore.exceptions;
using LearningBotCore.model;

namespace LearningBotCore.service.interfaces;

/// <summary>
/// Интерфейс для управления пользователями.
/// </summary>
public interface IUserManager
{
    /// <summary>
    /// Добавляет пользователя в систему.
    /// </summary>
    /// <param name="user">Объект пользователя для добавления.</param>
    void AddUser(User user);

    /// <summary>
    /// Возвращает пользователя по его идентификатору.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Объект пользователя.</returns>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь с указанным идентификатором не найден.</exception>
    User GetUser(long userId);
}