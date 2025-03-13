namespace Backend.Integrations.Interfaces;

/// <summary>
/// Интерфейс для сервиса управления токенами.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Получает токен доступа для API.
    /// </summary>
    /// <returns>Строка с токеном доступа.</returns>
    string GetAccessToken();
}