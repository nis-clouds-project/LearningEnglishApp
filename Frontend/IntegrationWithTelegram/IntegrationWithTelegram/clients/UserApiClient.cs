using System.Text;
using System.Text.Json;
using IntegrationWithTelegram.managers;
using IntegrationWithTelegram.models;

namespace IntegrationWithTelegram.clients
{
    /// <summary>
    /// Клиент для взаимодействия с API пользователей.
    /// </summary>
    public static class UserApiClient
    {
        // Базовый URL API для работы с пользователями.
        private static readonly string BaseUrl = "http://localhost:5000";

        // Статический HttpClient для выполнения HTTP-запросов.
        private static readonly HttpClient HttpClient = new();

        /// <summary>
        /// Добавляет пользователя через API.
        /// </summary>
        /// <param name="user">Объект пользователя для добавления.</param>
        /// <exception cref="Exception">Выбрасывается, если произошла ошибка при выполнении запроса.</exception>
        public static async void AddUser(User user)
        {
            // Сериализуем объект пользователя в JSON.
            var json = JsonSerializer.Serialize(user);

            // Создаем контент запроса с JSON-телом.
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Отправляем POST-запрос на эндпоинт добавления пользователя.
            var response = await HttpClient.PostAsync($"{BaseUrl}/api/user/add", content);

            // Проверяем ответ на наличие ошибок с помощью ExceptionHandler.
            ExceptionHandler.ValidException(response);
        }

        /// <summary>
        /// Получает пользователя по ID через API.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>Объект пользователя или null, если запрос не удался.</returns>
        /// <exception cref="Exception">Выбрасывается, если произошла ошибка при выполнении запроса.</exception>
        public static async Task<User?> GetUser(long userId)
        {
            // Отправляем GET-запрос на эндпоинт получения пользователя.
            var response = await HttpClient.GetAsync($"{BaseUrl}/api/user/get?userId={userId}");

            // Проверяем ответ на наличие ошибок с помощью ExceptionHandler.
            ExceptionHandler.ValidException(response);

            // Читаем и десериализуем ответ в объект User.
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(json);
        }
    }
}