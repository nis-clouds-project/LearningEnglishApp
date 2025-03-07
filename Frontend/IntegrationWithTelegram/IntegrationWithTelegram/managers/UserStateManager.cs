using IntegrationWithTelegram.models;

namespace IntegrationWithTelegram.managers;

/// <summary>
/// Менеджер для управления состоянием пользователей.
/// Хранит состояния пользователей в памяти и предоставляет методы для их установки и получения.
/// </summary>
public static class UserStateManager
{
    // Словарь для хранения состояний пользователей.
    // Ключ — идентификатор чата (long), значение — состояние (State).
    private static readonly Dictionary<long, State> UserStates = new();

    /// <summary>
    /// Устанавливает состояние для указанного пользователя.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="state">Новое состояние пользователя.</param>
    public static void SetState(long chatId, State state)
    {
        // Если состояние уже существует, оно будет перезаписано.
        UserStates[chatId] = state;
    }

    /// <summary>
    /// Получает текущее состояние пользователя.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <returns>Текущее состояние пользователя. Если состояние не найдено, возвращает <see cref="State.WaitingStart"/>.</returns>
    public static State GetState(long chatId)
    {
        // Возвращаем состояние пользователя или значение по умолчанию (WaitingStart).
        return UserStates.GetValueOrDefault(chatId, State.WaitingStart);
    }
}