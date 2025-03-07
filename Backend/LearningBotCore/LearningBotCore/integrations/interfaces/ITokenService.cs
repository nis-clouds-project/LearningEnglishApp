namespace LearningBotCore.integrations.interfaces;

/// <summary>
/// Интерфейс для сервиса работы с токенами доступа.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Получает текущий токен доступа. Если токен истек или отсутствует, запрашивает новый.
    /// </summary>
    /// <returns>Токен доступа.</returns>
    /// <exception cref="InvalidOperationException">Если не удалось получить токен.</exception>
    string GetAccessToken();
    
}