using Backend.Integrations.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Net.Http.Json;

namespace Backend.Integrations
{
    /// <summary>
    /// Сервис для работы с токенами доступа к Yandex Translate API.
    /// </summary>
    public class YandexTokenService : IYandexTokenService
    {
        private const string IAM_TOKEN_URL = "https://iam.api.cloud.yandex.net/iam/v1/tokens";
        
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<YandexTokenService> _logger;
        private readonly string _folderId;
        private readonly object _lockObject = new object();

        private string? _iamToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public YandexTokenService(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<YandexTokenService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
            
            _folderId = configuration["Yandex:FolderId"] 
                ?? throw new ArgumentNullException("Yandex:FolderId not configured");

            // Инициализируем токен при создании сервиса
            RequestNewToken().Wait();
        }

        /// <summary>
        /// Получает текущий IAM токен. Если токен истек или отсутствует, запрашивает новый.
        /// </summary>
        public string GetIamToken()
        {
            lock (_lockObject)
            {
                if (string.IsNullOrEmpty(_iamToken) || DateTime.UtcNow >= _tokenExpiry)
                {
                    _logger.LogInformation("IAM токен истек или отсутствует. Запрашиваем новый...");
                    RequestNewToken().Wait();
                }

                return _iamToken ?? throw new InvalidOperationException("Failed to obtain IAM token");
            }
        }

        /// <summary>
        /// Получает идентификатор каталога Yandex Cloud
        /// </summary>
        public string GetFolderId() => _folderId;

        /// <summary>
        /// Запрашивает новый IAM токен
        /// </summary>
        private async Task RequestNewToken()
        {
            try
            {
                _logger.LogInformation("Начало запроса нового IAM токена");

                var oauthToken = _configuration["Yandex:OAuthToken"] 
                    ?? throw new ArgumentNullException("Yandex:OAuthToken not configured");

                var request = new
                {
                    yandexPassportOauthToken = oauthToken
                };

                var response = await _httpClient.PostAsJsonAsync(IAM_TOKEN_URL, request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
                
                if (result == null || string.IsNullOrEmpty(result.IamToken))
                {
                    throw new InvalidOperationException("Invalid response format: missing IAM token");
                }

                lock (_lockObject)
                {
                    _iamToken = result.IamToken;
                    // Токен действителен 12 часов, устанавливаем истечение через 11 часов для безопасности
                    _tokenExpiry = DateTime.UtcNow.AddHours(11);
                }

                _logger.LogInformation("IAM токен успешно обновлен. Истекает: {Expiry}", _tokenExpiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при запросе нового IAM токена");
                throw;
            }
        }

        /// <summary>
        /// Запускает периодическое обновление токена
        /// </summary>
        public async Task StartTokenRefreshAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Проверяем, нужно ли обновить токен
                    if (DateTime.UtcNow >= _tokenExpiry.AddMinutes(-30))
                    {
                        await RequestNewToken();
                    }

                    // Ждем 15 минут перед следующей проверкой
                    await Task.Delay(TimeSpan.FromMinutes(15), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Отмена периодического обновления токена");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при периодическом обновлении токена");
                    // Ждем 1 минуту перед повторной попыткой в случае ошибки
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }
            }
        }

        public async Task<string> GetIamTokenAsync()
        {
            if (string.IsNullOrEmpty(_iamToken))
            {
                await RequestNewToken();
            }
            return _iamToken;
        }

        private class TokenResponse
        {
            public string IamToken { get; set; } = string.Empty;
        }
    }
}