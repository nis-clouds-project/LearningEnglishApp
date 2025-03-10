namespace Backend.Integrations.Interfaces;

/// <summary>
/// Интерфейс для сервиса управления токенами доступа.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Получает действующий токен доступа.
    /// Если текущий токен истек, автоматически запрашивает новый.
    /// </summary>
    /// <returns>Токен доступа.</returns>
    string GetAccessToken();
}