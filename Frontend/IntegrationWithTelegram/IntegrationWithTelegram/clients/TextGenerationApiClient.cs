using System.Text;
using System.Text.Json;
using IntegrationWithTelegram.managers;
using IntegrationWithTelegram.models;

namespace IntegrationWithTelegram.clients
{
    /// <summary>
    /// Клиент для взаимодействия с API генерации текста.
    /// </summary>
    public static class TextGenerationApiClient
    {
        // Базовый URL API для генерации текста.
        private static readonly string BaseUrl = "http://localhost:5000";

        // Статический HttpClient для выполнения HTTP-запросов.
        private static readonly HttpClient HttpClient = new();

        /// <summary>
        /// Генерирует текст на основе списка слов.
        /// </summary>
        /// <param name="words">Список слов для генерации текста.</param>
        /// <returns>Сгенерированный текст.</returns>
        /// <exception cref="Exception">Выбрасывается, если произошла ошибка при выполнении запроса.</exception>
        public static async Task<string?> GenerateTextAsync(List<Word> words)
        {
            // Сериализуем список слов в JSON.
            var json = JsonSerializer.Serialize(words);

            // Создаем контент запроса с JSON-телом.
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Отправляем POST-запрос на эндпоинт генерации текста.
            var response = await HttpClient.PostAsync($"{BaseUrl}/api/text-generation/generate", content);

            // Проверяем ответ на наличие ошибок с помощью ExceptionHandler.
            ExceptionHandler.ValidException(response);

            // Читаем и десериализуем ответ в строку (сгенерированный текст).
            var generatedText = await response.Content.ReadAsStringAsync();
            return generatedText;
        }
    }
}