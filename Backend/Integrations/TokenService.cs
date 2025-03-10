using Backend.Integrations.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Backend.Integrations
{
    /// <summary>
    /// Сервис для работы с токенами доступа к GigaChat API.
    /// </summary>
    public class TokenService : ITokenService
    {
        // Константы для URL и авторизации
        private const string UrlToGetToken = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

        private const string Authorization =
            "Basic MTUzZjY0YWItZmM1ZC00NTg2LTlmYTYtNDI2MjllMzY0NWY3OjM5N2RlZGZhLTZkYWMtNGQyNi1iZTMzLTNlNzNlYTUwNWIzZQ==";

        // Поля для хранения токена и времени его истечения
        private string? _accessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly ILogger<TokenService> _logger;

        public TokenService(ILogger<TokenService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Получает текущий токен доступа. Если токен истек или отсутствует, запрашивает новый.
        /// </summary>
        /// <returns>Токен доступа.</returns>
        /// <exception cref="InvalidOperationException">Если не удалось получить токен.</exception>
        public string GetAccessToken()
        {
            if (string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiry)
            {
                _logger.LogInformation("Токен истек или отсутствует. Запрашиваем новый...");
                RequestNewToken();
            }

            return _accessToken!;
        }

        /// <summary>
        /// Запрашивает новый токен доступа у сервера.
        /// </summary>
        /// <exception cref="InvalidOperationException">Если запрос на получение токена завершился ошибкой.</exception>
        private void RequestNewToken()
        {
            try
            {
                var options = new RestClientOptions(UrlToGetToken)
                {
                    RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                    {
                        _logger.LogInformation("Проверка SSL сертификата. Ошибки: {Errors}", errors);
                        return true; // Принимаем любой сертификат
                    },
                    MaxTimeout = 10000 // 10 секунд таймаут
                };

                var client = new RestClient(options);
                var request = BuildRequest();

                _logger.LogInformation("Отправка запроса на получение токена");
                var response = client.Execute(request);
                _logger.LogInformation("Получен ответ. Статус: {Status}", response.StatusCode);

                if (!response.IsSuccessful)
                {
                    _logger.LogError("Ошибка при получении токена. Статус: {Status}, Ошибка: {Error}, Содержимое: {Content}", 
                        response.StatusCode, response.ErrorMessage, response.Content);
                    throw new InvalidOperationException($"Ошибка при получении токена: {response.ErrorMessage}");
                }

                if (string.IsNullOrEmpty(response.Content))
                {
                    _logger.LogError("Получен пустой ответ от сервера");
                    throw new InvalidOperationException("Пустой ответ от сервера при запросе токена.");
                }

                _logger.LogInformation("Десериализация ответа: {Content}", response.Content);
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(response.Content);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    _logger.LogError("Неверный формат ответа от сервера");
                    throw new InvalidOperationException("Неверный формат ответа от сервера: отсутствует токен.");
                }

                _accessToken = tokenResponse.AccessToken;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300);
                _logger.LogInformation("Токен успешно получен. Истекает: {Expiry}", _tokenExpiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при запросе нового токена");
                throw;
            }
        }

        /// <summary>
        /// Создает и настраивает запрос для получения токена.
        /// </summary>
        /// <returns>Настроенный запрос.</returns>
        private RestRequest BuildRequest()
        {
            var request = new RestRequest("", Method.Post);

            // Добавляем заголовки
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("RqUID", Guid.NewGuid().ToString());
            request.AddHeader("Authorization", Authorization);

            // Добавляем тело запроса
            request.AddParameter("scope", "GIGACHAT_API_PERS");

            return request;
        }

        /// <summary>
        /// Класс для десериализации ответа с токеном.
        /// </summary>
        private class TokenResponse
        {
            [JsonProperty("access_token")] public string AccessToken { get; set; } = null!;

            [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
        }
    }
}