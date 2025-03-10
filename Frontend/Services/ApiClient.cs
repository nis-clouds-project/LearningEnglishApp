using System.Net.Http.Json;
using Frontend.Models;

namespace Frontend.Services
{
    /// <summary>
    /// Клиент для взаимодействия с API бэкенда.
    /// </summary>
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private bool _disposed;

        public ApiClient(string baseUrl)
        {
            Console.WriteLine($"Инициализация ApiClient с базовым URL: {baseUrl}");
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            Console.WriteLine("HttpClient настроен с таймаутом 30 секунд");
        }

        /// <summary>
        /// Получает список изученных слов пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="category">Категория слов (опционально).</param>
        /// <returns>Список изученных слов, сгруппированных по категориям.</returns>
        public async Task<List<VocabularyCategory>?> GetLearnedWordsAsync(long userId, string? category = null)
        {
            try
            {
                var url = $"/api/Word/learned?userId={userId}";
                if (!string.IsNullOrEmpty(category))
                {
                    url += $"&category={category}";
                }

                var fullUrl = new Uri(_httpClient.BaseAddress!, url).ToString();
                Console.WriteLine($"[GetLearnedWordsAsync] Отправка GET запроса: {fullUrl}");

                using var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"[GetLearnedWordsAsync] Получен ответ: {(int)response.StatusCode} {response.StatusCode}");
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GetLearnedWordsAsync] Содержимое ответа: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try 
                    {
                        var vocabulary = await response.Content.ReadFromJsonAsync<List<VocabularyCategory>>();
                        Console.WriteLine($"[GetLearnedWordsAsync] Получен словарь с {vocabulary?.Count ?? 0} категориями");
                        if (vocabulary != null)
                        {
                            foreach (var vocabularyCategory in vocabulary)
                            {
                                Console.WriteLine($"[GetLearnedWordsAsync] Категория '{vocabularyCategory.Category}' содержит {vocabularyCategory.Words?.Count ?? 0} слов");
                            }
                        }
                        return vocabulary;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GetLearnedWordsAsync] Ошибка при десериализации JSON: {ex.Message}");
                        Console.WriteLine($"[GetLearnedWordsAsync] Полученный JSON: {responseContent}");
                        return null;
                    }
                }

                Console.WriteLine($"[GetLearnedWordsAsync] Ошибка при получении словаря: {responseContent}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetLearnedWordsAsync] Исключение при получении словаря:");
                Console.WriteLine($"[GetLearnedWordsAsync] {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[GetLearnedWordsAsync] Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        public class GeneratedTextResponse
        {
            public string EnglishText { get; set; } = string.Empty;
            public string RussianText { get; set; } = string.Empty;
            public Dictionary<string, string> Words { get; set; } = new();
        }

        /// <summary>
        /// Генерирует текст на основе слов из словаря пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>Сгенерированный текст или null в случае ошибки.</returns>
        public async Task<GeneratedTextResponse?> GenerateTextFromVocabularyAsync(long userId)
        {
            try
            {
                var url = $"/api/Word/generate-text?userId={userId}";
                Console.WriteLine($"[GenerateTextFromVocabularyAsync] Отправка GET запроса: {url}");
                
                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"[GenerateTextFromVocabularyAsync] Получен ответ: {(int)response.StatusCode} {response.StatusCode}");
                
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GenerateTextFromVocabularyAsync] Содержимое ответа: {responseContent}");
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[GenerateTextFromVocabularyAsync] Ошибка при генерации текста. Статус: {response.StatusCode}");
                    return null;
                }

                try
                {
                    var result = await response.Content.ReadFromJsonAsync<GeneratedTextResponse>();
                    if (result != null)
                    {
                        Console.WriteLine($"[GenerateTextFromVocabularyAsync] Успешно получен текст:");
                        Console.WriteLine($"[GenerateTextFromVocabularyAsync] Английский текст: {result.EnglishText}");
                        Console.WriteLine($"[GenerateTextFromVocabularyAsync] Русский текст: {result.RussianText}");
                        Console.WriteLine($"[GenerateTextFromVocabularyAsync] Количество слов: {result.Words.Count}");
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GenerateTextFromVocabularyAsync] Ошибка при десериализации ответа: {ex.Message}");
                    Console.WriteLine($"[GenerateTextFromVocabularyAsync] Содержимое ответа: {responseContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GenerateTextFromVocabularyAsync] Ошибка при генерации текста для пользователя {userId}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[GenerateTextFromVocabularyAsync] Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        /// <summary>
        /// Проверяет существование пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>true, если пользователь существует; иначе false.</returns>
        public async Task<bool> UserExistsAsync(long userId)
        {
            try
            {
                var url = $"/api/User/exists?userId={userId}";
                var fullUrl = new Uri(_httpClient.BaseAddress!, url).ToString();
                Console.WriteLine($"Отправка GET запроса: {fullUrl}");
                
                using var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"Получен ответ: {(int)response.StatusCode} {response.StatusCode}");
                Console.WriteLine($"Заголовки ответа: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}"))}");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<bool>();
                    Console.WriteLine($"Результат проверки существования пользователя: {result}");
                    return result;
                }
                
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка при проверке существования пользователя: {error}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение при проверке существования пользователя: {ex}");
                if (ex is HttpRequestException httpEx)
                {
                    Console.WriteLine($"HttpRequestException StatusCode: {httpEx.StatusCode}");
                }
                return false;
            }
        }

        /// <summary>
        /// Добавляет нового пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>true, если пользователь успешно добавлен; иначе false.</returns>
        public async Task<bool> AddUserAsync(long userId)
        {
            try
            {
                var url = "/api/User/add";
                var fullUrl = new Uri(_httpClient.BaseAddress!, url).ToString();
                Console.WriteLine($"Отправка POST запроса: {fullUrl}");
                
                using var response = await _httpClient.PostAsJsonAsync(url, userId);
                Console.WriteLine($"Получен ответ: {(int)response.StatusCode} {response.StatusCode}");
                Console.WriteLine($"Заголовки ответа: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}"))}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка при добавлении пользователя: {error}");
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение при добавлении пользователя: {ex}");
                if (ex is HttpRequestException httpEx)
                {
                    Console.WriteLine($"HttpRequestException StatusCode: {httpEx.StatusCode}");
                }
                return false;
            }
        }

        /// <summary>
        /// Получает случайное слово для изучения.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="category">Категория слова.</param>
        /// <returns>Случайное слово или null в случае ошибки.</returns>
        public async Task<Word?> GetRandomWordAsync(long userId, string category)
        {
            try
            {
                var url = $"/api/Word/random?userId={userId}&category={category}";
                var fullUrl = new Uri(_httpClient.BaseAddress!, url).ToString();
                Console.WriteLine($"Отправка GET запроса: {fullUrl}");
                
                using var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"Получен ответ: {(int)response.StatusCode} {response.StatusCode}");
                Console.WriteLine($"Заголовки ответа: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}"))}");
                
                if (response.IsSuccessStatusCode)
                {
                    var word = await response.Content.ReadFromJsonAsync<Word>();
                    Console.WriteLine($"Получено слово: {word?.ToString()}");
                    return word;
                }
                
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка при получении случайного слова: {error}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение при получении случайного слова: {ex}");
                if (ex is HttpRequestException httpEx)
                {
                    Console.WriteLine($"HttpRequestException StatusCode: {httpEx.StatusCode}");
                }
                return null;
            }
        }

        /// <summary>
        /// Добавляет слово в словарь пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="wordId">Идентификатор слова.</param>
        /// <returns>true, если слово успешно добавлено; иначе false.</returns>
        public async Task<bool> AddWordToVocabularyAsync(long userId, int wordId)
        {
            try
            {
                // Сначала проверяем существование пользователя
                var userExists = await UserExistsAsync(userId);
                if (!userExists)
                {
                    Console.WriteLine($"[AddWordToVocabularyAsync] Пользователь {userId} не существует, пробуем создать");
                    var userAdded = await AddUserAsync(userId);
                    if (!userAdded)
                    {
                        Console.WriteLine($"[AddWordToVocabularyAsync] Не удалось создать пользователя {userId}");
                        return false;
                    }
                    Console.WriteLine($"[AddWordToVocabularyAsync] Пользователь {userId} успешно создан");
                }

                // Отправляем параметры в URL
                var url = $"/api/Word/vocabulary/add?userId={userId}&wordId={wordId}";
                var fullUrl = new Uri(_httpClient.BaseAddress!, url).ToString();
                Console.WriteLine($"[AddWordToVocabularyAsync] Отправка POST запроса: {fullUrl}");
                
                // Отправляем пустое тело запроса
                using var response = await _httpClient.PostAsync(url, null);
                Console.WriteLine($"[AddWordToVocabularyAsync] Получен ответ: {(int)response.StatusCode} {response.StatusCode}");
                
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[AddWordToVocabularyAsync] Содержимое ответа: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[AddWordToVocabularyAsync] Ошибка при добавлении слова в словарь: {responseContent}");
                    return false;
                }
                
                Console.WriteLine($"[AddWordToVocabularyAsync] Слово успешно добавлено в словарь");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddWordToVocabularyAsync] Исключение при добавлении слова в словарь:");
                Console.WriteLine($"[AddWordToVocabularyAsync] {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[AddWordToVocabularyAsync] Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        public async Task<Word?> GetWordByIdAsync(int wordId)
        {
            try
            {
                var url = $"/api/Word/{wordId}";
                var fullUrl = new Uri(_httpClient.BaseAddress!, url).ToString();
                Console.WriteLine($"Отправка GET запроса для получения слова: {fullUrl}");

                using var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"Получен ответ: {(int)response.StatusCode} {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var word = await response.Content.ReadFromJsonAsync<Word>();
                    Console.WriteLine($"Получено слово: {word?.ToString()}");
                    return word;
                }

                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка при получении слова: {error}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение при получении слова: {ex}");
                return null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _httpClient.Dispose();
            }

            _disposed = true;
        }
    }
} 