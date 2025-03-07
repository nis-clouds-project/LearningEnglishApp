using LearningBotCore.integrations.interfaces;
using Newtonsoft.Json;
using RestSharp;

namespace LearningBotCore.integrations
{
    /// <summary>
    /// Сервис для работы с токенами доступа к GigaChat API.
    /// </summary>
    public class TokenService : ITokenService
    {
        // Константы для URL и авторизации
        private const string UrlToGetToken = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

        private const string Authorization =
            "Basic NTAwYTgwNjktMDVmMS00YzAyLTlkZjMtNTc3NjRjZGYyNjJkOmUyYTIwYmQ1LTMyYmUtNDIzNC04ZmI2LTA5Y2Y2MzBkYTFjYQ==";

        // Поля для хранения токена и времени его истечения
        private string? _accessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

        /// <summary>
        /// Получает текущий токен доступа. Если токен истек или отсутствует, запрашивает новый.
        /// </summary>
        /// <returns>Токен доступа.</returns>
        /// <exception cref="InvalidOperationException">Если не удалось получить токен.</exception>
        public string GetAccessToken()
        {
            if (string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiry)
            {
                Console.WriteLine("Токен истек или отсутствует. Запрашиваем новый...");
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
            var client = new RestClient(UrlToGetToken);
            var request = BuildRequest();

            var response = client.Execute(request);

            // Проверяем успешность запроса
            if (!response.IsSuccessful)
            {
                throw new InvalidOperationException($"Ошибка при получении токена: {response.ErrorMessage}");
            }

            // Проверяем наличие содержимого в ответе
            if (string.IsNullOrEmpty(response.Content))
            {
                throw new InvalidOperationException("Пустой ответ от сервера при запросе токена.");
            }

            // Десериализация ответа

            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(response.Content);


            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("Неверный формат ответа от сервера: отсутствует токен.");
            }

            // Сохраняем токен и время его истечения
            _accessToken = tokenResponse.AccessToken;
            _tokenExpiry =
                DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300); // Обновляем токен за 5 минут до истечения
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