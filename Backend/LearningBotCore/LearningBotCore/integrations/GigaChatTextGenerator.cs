using LearningBotCore.integrations.interfaces;
using LearningBotCore.model;
using RestSharp;
using JsonElement = System.Text.Json.JsonElement;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace LearningBotCore.integrations
{
    /// <summary>
    /// Класс для генерации текста с использованием GigaChat API.
    /// Реализует интерфейс <see cref="ITextGenerator"/> для генерации текста на основе списка слов.
    /// </summary>
    public class GigaChatTextGenerator : ITextGenerator
    {
        // URL для отправки запросов к GigaChat API
        private const string UrlToMakeRequest = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

        // Сервис для получения токена доступа
        private static readonly ITokenService TokenService = new TokenService();

        /// <summary>
        /// Генерирует текст на основе списка слов, используя GigaChat API.
        /// </summary>
        /// <param name="words">Список слов, на основе которых будет сгенерирован текст.</param>
        /// <returns>Сгенерированный текст.</returns>
        /// <exception cref="InvalidOperationException">Если запрос к API завершился ошибкой.</exception>
        /// <exception cref="JsonException">Если не удалось десериализовать ответ сервера.</exception>
        public string GenerateText(List<Word> words)
        {
            // Формируем промпт для запроса к API
            string promt = BuildPromt(words);

            try
            {
                // Получаем токен доступа
                string accessToken = TokenService.GetAccessToken();

                // Создаем клиент для отправки запросов
                var client = new RestClient(UrlToMakeRequest);

                // Создаем и настраиваем запрос
                var request = BuildRequest(accessToken, promt);

                // Отправляем запрос и получаем ответ
                var response = client.Execute(request);

                // Проверяем успешность запроса
                if (!response.IsSuccessful)
                {
                    throw new InvalidOperationException($"Ошибка при выполнении запроса: {response.ErrorMessage}");
                }

                // Извлекаем сгенерированный текст из ответа сервера
                return GetAnswer(response);
            }
            catch (Exception ex)
            {
                // Обрабатываем исключения и выбрасываем новое исключение с понятным сообщением
                throw new InvalidOperationException("Ошибка при генерации текста.", ex);
            }
        }

        /// <summary>
        /// Создает и настраивает запрос к GigaChat API.
        /// </summary>
        /// <param name="accessToken">Токен доступа для авторизации.</param>
        /// <param name="message">Сообщение (промпт), которое будет отправлено в API.</param>
        /// <returns>Настроенный запрос.</returns>
        private RestRequest BuildRequest(string accessToken, string message)
        {
            // Создаем POST-запрос
            var request = new RestRequest("", Method.Post);

            // Добавляем заголовки
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("Content-Type", "application/json");

            // Формируем тело запроса в формате JSON
            var requestBody = new
            {
                model = "GigaChat", // Указываем модель, которую используем
                messages = new[]
                {
                    new
                    {
                        role = "user", // Роль пользователя
                        content = message // Сообщение пользователя (промпт)
                    }
                },
                temperature = 0.7, // Параметр случайности (от 0 до 1)
                max_tokens = 400 // Максимальное количество токенов в ответе
            };

            // Добавляем тело запроса в формате JSON
            request.AddJsonBody(requestBody);
            return request;
        }

        /// <summary>
        /// Извлекает сгенерированный текст из ответа сервера.
        /// </summary>
        /// <param name="response">Ответ сервера.</param>
        /// <returns>Сгенерированный текст.</returns>
        /// <exception cref="JsonException">Если не удалось десериализовать ответ сервера.</exception>
        /// <exception cref="InvalidOperationException">Если ответ сервера не содержит ожидаемых данных.</exception>
        private string GetAnswer(RestResponse response)
        {
            // Проверяем, что ответ не пустой
            if (response.Content == null)
            {
                throw new InvalidOperationException("Пустой ответ от сервера.");
            }

            // Логируем ответ сервера для отладки
            Console.WriteLine("Ответ от сервера:");
            Console.WriteLine(response.Content);

            try
            {
                // Парсим JSON-ответ
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response.Content);

                // Извлекаем сгенерированный текст из JSON
                var generatedText = jsonResponse
                    .GetProperty("choices")[0] // Первый элемент массива choices
                    .GetProperty("message") // Объект message
                    .GetProperty("content") // Поле content
                    .GetString(); // Получаем значение как строку

                // Проверяем, что текст не пустой
                if (string.IsNullOrEmpty(generatedText))
                {
                    throw new InvalidOperationException("Ответ сервера не содержит сгенерированного текста.");
                }

                return generatedText;
            }
            catch (JsonException ex)
            {
                // Обрабатываем ошибки десериализации
                throw new JsonException("Ошибка десериализации ответа сервера.", ex);
            }
            catch (Exception ex)
            {
                // Обрабатываем другие ошибки
                throw new InvalidOperationException("Ошибка при обработке ответа сервера.", ex);
            }
        }

        /// <summary>
        /// Формирует промпт для запроса к API на основе списка слов.
        /// </summary>
        /// <param name="words">Список слов, которые должны быть использованы в тексте.</param>
        /// <returns>Сформированный промпт.</returns>
        /// <exception cref="ArgumentException">Если список слов пуст.</exception>
        public string BuildPromt(List<Word> words)
        {
            // Проверяем, что список слов не пустой
            if (words == null || words.Count == 0)
            {
                throw new ArgumentException("Список слов не может быть пустым.", nameof(words));
            }

            // Формируем промпт для ИИ
            var prompt = CreatePrompt(words);

            // Возвращаем промпт
            return prompt;
        }

        /// <summary>
        /// Создает промпт для запроса к API, используя список слов.
        /// </summary>
        /// <param name="words">Список слов, которые должны быть использованы в тексте.</param>
        /// <returns>Сформированный промпт.</returns>
        private string CreatePrompt(List<Word> words)
        {
            // Формируем строку с ключевыми словами
            var keywords = string.Join(", ", words);

            // Создаем промпт для ИИ
            var prompt =
                $"Напиши текст, состоящий из русской версии рассказа и английской, так чтобы на английском использовались следующие слова как можно чаще: {keywords}";

            return prompt;
        }
    }
}