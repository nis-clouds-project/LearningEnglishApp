using System.Text.Json;
using IntegrationWithTelegram.managers;
using IntegrationWithTelegram.models;

namespace IntegrationWithTelegram.clients
{
    /// <summary>
    /// Клиент для взаимодействия с API работы со словами.
    /// </summary>
    public static class WordApiClient
    {
        // Базовый URL API для работы со словами.
        private static readonly string BaseUrl = "http://localhost:5000";

        // Статический HttpClient для выполнения HTTP-запросов.
        private static readonly HttpClient HttpClient = new();

        /// <summary>
        /// Получает список случайных слов для генерации текста.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="category">Категория слов (опционально).</param>
        /// <returns>Список слов.</returns>
        /// <exception cref="Exception">Выбрасывается, если произошла ошибка при выполнении запроса.</exception>
        public static async Task<List<Word>?> GetRandomWordsForGeneratingTextAsync(long userId,
            CategoryType? category = null)
        {
            // Формируем URL с параметрами.
            var url = $"{BaseUrl}/api/word/random-words?userId={userId}";
            if (category.HasValue)
            {
                url += $"&category={category}";
            }

            // Отправляем GET-запрос.
            var response = await HttpClient.GetAsync(url);

            // Проверяем ответ на наличие ошибок с помощью ExceptionHandler.
            ExceptionHandler.ValidException(response);

            // Читаем и десериализуем ответ в список слов.
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Word>>(json);
        }

        /// <summary>
        /// Добавляет слово в словарь пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="wordId">Идентификатор слова.</param>
        /// <returns>True, если запрос выполнен успешно, иначе — False.</returns>
        /// <exception cref="Exception">Выбрасывается, если произошла ошибка при выполнении запроса.</exception>
        public static async Task AddWordInUserVocabularyAsync(long userId, int wordId)
        {
            // Формируем URL с параметрами.
            var url = $"{BaseUrl}/api/word/add-word?userId={userId}&wordId={wordId}";

            // Отправляем POST-запрос.
            var response = await HttpClient.PostAsync(url, null);

            // Проверяем ответ на наличие ошибок с помощью ExceptionHandler.
            ExceptionHandler.ValidException(response);
        }

        /// <summary>
        /// Получает случайное слово для изучения.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="category">Категория слова.</param>
        /// <returns>Идентификатор слова.</returns>
        /// <exception cref="Exception">Выбрасывается, если произошла ошибка при выполнении запроса.</exception>
        public static async Task<int?> GetRandomWordForLearningAsync(long userId, CategoryType category)
        {
            // Формируем URL с параметрами.
            var url = $"{BaseUrl}/api/word/random-word?userId={userId}&category={category}";

            // Отправляем GET-запрос.
            var response = await HttpClient.GetAsync(url);

            // Проверяем ответ на наличие ошибок с помощью ExceptionHandler.
            ExceptionHandler.ValidException(response);

            // Читаем и десериализуем ответ в идентификатор слова.
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<int>(json);
        }

        /// <summary>
        /// Получает слово по его идентификатору.
        /// </summary>
        /// <param name="wordId">Идентификатор слова.</param>
        /// <returns>Объект слова.</returns>
        /// <exception cref="Exception">Выбрасывается, если произошла ошибка при выполнении запроса.</exception>
        public static async Task<Word?> GetWordByIdAsync(int wordId)
        {
            // Формируем URL с параметрами.
            var url = $"{BaseUrl}/api/word/word-by-id?wordId={wordId}";

            // Отправляем GET-запрос.
            var response = await HttpClient.GetAsync(url);

            // Проверяем ответ на наличие ошибок с помощью ExceptionHandler.
            ExceptionHandler.ValidException(response);

            // Читаем и десериализуем ответ в объект слова.
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Word>(json);
        }

        /// <summary>
        /// Добавляет пользовательское слово в словарь пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="word">Текст слова.</param>
        /// <returns>True, если запрос выполнен успешно, иначе — False.</returns>
        /// <exception cref="Exception">Выбрасывается, если произошла ошибка при выполнении запроса.</exception>
        public static async Task AddCustomWordInUserVocabularyAsync(long userId, string word)
        {
            // Формируем URL с параметрами.
            var url = $"{BaseUrl}/api/word/add-custom-word?userId={userId}&word={word}";

            // Отправляем POST-запрос.
            var response = await HttpClient.PostAsync(url, null);

            // Проверяем ответ на наличие ошибок с помощью ExceptionHandler.
            ExceptionHandler.ValidException(response);
        }
    }
}