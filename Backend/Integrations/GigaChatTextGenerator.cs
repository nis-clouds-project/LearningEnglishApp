using Backend.Integrations.Interfaces;
using Backend.Models;
using RestSharp;

using JsonElement = System.Text.Json.JsonElement;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Backend.Integrations
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
        /// Генерирует текст на основе списка слов.
        /// </summary>
        public async Task<string> GenerateTextAsync(IEnumerable<string> words)
        {
            try
            {
                var wordList = words.ToList();
                if (!wordList.Any())
                {
                    return "No words provided for text generation.";
                }

                // Формируем промпт для запроса к API
                string prompt = $"Generate a text in English using the following words: {string.Join(", ", wordList)}. " +
                              "The text should be natural and engaging, using the provided words in context.";

                return await GenerateTextWithPrompt(prompt);
            }
            catch (Exception ex)
            {
                return $"Failed to generate text using GigaChat API. Using fallback: Here are the words: {string.Join(", ", words)}.";
            }
        }

        /// <summary>
        /// Генерирует текст на основе слов с их переводами.
        /// </summary>
        public async Task<string> GenerateTextWithTranslationsAsync(IDictionary<string, string> wordsWithTranslations)
        {
            try
            {
                if (!wordsWithTranslations.Any())
                {
                    return "No words provided for text generation.";
                }

                // Формируем промпт для запроса к API с учетом переводов
                var wordPairs = wordsWithTranslations.Select(w => $"{w.Key} ({w.Value})");
                string prompt = "Create a bilingual story in English and Russian. " +
                              $"Use these English words with their Russian translations: {string.Join(", ", wordPairs)}. " +
                              "First write a paragraph in English using all the English words naturally in context. " +
                              "Then provide a Russian translation of the same story, incorporating the Russian translations. " +
                              "Make the story engaging and connected.";

                return await GenerateTextWithPrompt(prompt);
            }
            catch (Exception ex)
            {
                var sentences = new List<string>();
                foreach (var pair in wordsWithTranslations)
                {
                    sentences.Add($"The word '{pair.Key}' means '{pair.Value}' in Russian.");
                }
                return string.Join("\n", sentences);
            }
        }

        private async Task<string> GenerateTextWithPrompt(string prompt)
        {
            try
            {
                // Получаем токен доступа
                string accessToken = TokenService.GetAccessToken();

                // Создаем клиент для отправки запросов
                var client = new RestClient(UrlToMakeRequest);

                // Создаем и настраиваем запрос
                var request = BuildRequest(accessToken, prompt);

                // Отправляем запрос и получаем ответ
                var response = client.Execute(request);

                // Проверяем успешность запроса
                if (!response.IsSuccessful)
                {
                    throw new InvalidOperationException($"Error during request: {response.ErrorMessage}");
                }

                // Извлекаем сгенерированный текст из ответа сервера
                return GetAnswer(response);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to generate text with GigaChat API.", ex);
            }
        }

        /// <summary>
        /// Создает и настраивает запрос к GigaChat API.
        /// </summary>
        private RestRequest BuildRequest(string accessToken, string message)
        {
            var request = new RestRequest("", Method.Post);

            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("Content-Type", "application/json");

            var requestBody = new
            {
                model = "GigaChat",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = message
                    }
                },
                temperature = 0.7,
                max_tokens = 1000
            };

            request.AddJsonBody(requestBody);
            return request;
        }

        /// <summary>
        /// Извлекает сгенерированный текст из ответа сервера.
        /// </summary>
        private string GetAnswer(RestResponse response)
        {
            if (response.Content == null)
            {
                throw new InvalidOperationException("Empty response from server.");
            }

            try
            {
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var generatedText = jsonResponse
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrEmpty(generatedText))
                {
                    throw new InvalidOperationException("Server response does not contain generated text.");
                }

                return generatedText;
            }
            catch (JsonException ex)
            {
                throw new JsonException("Error deserializing server response.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error processing server response.", ex);
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