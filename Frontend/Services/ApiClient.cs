using System.Net.Http.Json;
using Frontend.Models;
using Newtonsoft.Json;

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
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// Получает список категорий слов.
        /// </summary>
        /// <returns>Список категорий слов или null в случае ошибки.</returns>
        public async Task<List<Category>?> GetCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Word/categories");

                if (response.IsSuccessStatusCode)
                {
                    var categories = await response.Content.ReadFromJsonAsync<List<Category>>();
                    return categories;
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Получает список изученных слов пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="categoryId">Идентификатор категории (опционально).</param>
        /// <returns>Список изученных слов или null в случае ошибки.</returns>
        public async Task<List<Word>?> GetLearnedWordsAsync(long userId, long? categoryId = null)
        {
            try
            {
                var url = categoryId.HasValue
                    ? $"api/Word/learned?userId={userId}&categoryId={categoryId}"
                    : $"api/Word/learned?userId={userId}";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var words = await response.Content.ReadFromJsonAsync<List<Word>>();
                    return words;
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
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
                var response = await _httpClient.GetAsync($"api/TextGeneration/generate?userId={userId}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GeneratedTextResponse>();
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
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
                var response = await _httpClient.GetAsync($"api/User/exists?userId={userId}");

                if (response.IsSuccessStatusCode)
                {
                    var exists = await response.Content.ReadFromJsonAsync<bool>();
                    return exists;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Добавляет нового пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>true, если пользователь успешно добавлен; иначе false.</returns>
        public async Task<User?> AddUserAsync(long userId)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/User/add", userId);

                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<User>();
                    return user;
                }

                var error = await response.Content.ReadAsStringAsync();
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Получает случайное слово для изучения.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="categoryId">Идентификатор категории (опционально).</param>
        /// <returns>Случайное слово или null в случае ошибки.</returns>
        public async Task<Word?> GetRandomWordAsync(long userId, long? categoryId = null)
        {
            try
            {
                var url = categoryId.HasValue
                    ? $"api/Word/random?userId={userId}&categoryId={categoryId}"
                    : $"api/Word/random?userId={userId}";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var word = await response.Content.ReadFromJsonAsync<Word>();
                    return word;
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Добавляет слово в словарь пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="wordId">Идентификатор слова.</param>
        /// <returns>true, если слово успешно добавлено; иначе false.</returns>
        public async Task<bool> AddWordToVocabularyAsync(long userId, long wordId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/Word/vocabulary/add?userId={userId}&wordId={wordId}", null);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Получает слово по его идентификатору.
        /// </summary>
        public async Task<Word?> GetWordByIdAsync(long wordId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Word/{wordId}");

                if (response.IsSuccessStatusCode)
                {
                    var word = await response.Content.ReadFromJsonAsync<Word>();
                    return word;
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<Word?> AddCustomWordAsync(long userId, string text, string translation)
        {
            try
            {
                var request = new CustomWordRequest
                {
                    UserId = userId,
                    Text = text?.Trim(),
                    Translation = translation?.Trim()
                };

                var response = await _httpClient.PostAsJsonAsync("api/Word/custom", request);

                if (response.IsSuccessStatusCode)
                {
                    var word = await response.Content.ReadFromJsonAsync<Word>();
                    return word;
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<Word?> GetRandomCustomWordAsync(long userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Word/custom/random?userId={userId}");

                if (response.IsSuccessStatusCode)
                {
                    var word = await response.Content.ReadFromJsonAsync<Word>();
                    return word;
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<Word>?> GetAllCustomWordsAsync(long userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Word/custom?userId={userId}");
                if (response.IsSuccessStatusCode)
                {
                    var word = await response.Content.ReadFromJsonAsync<List<Word>>();
                    return word;
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> DeleteCustomWord(long userId, long wordId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/Word/custom?userId={userId}&wordId={wordId}");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        public async Task<string?> GetLocalTranslationAsync(string sourceWord, string direction)
        {
            var url = $"api/word/local-translate?word={Uri.EscapeDataString(sourceWord)}&direction={direction}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
            if (dict != null && dict.TryGetValue("result", out var translated))
            {
                return translated;
            }

            return null;
        }

        public async Task<string?> GetLocalTranslationAsync(string sourceWord, string direction)
        {
            var url = $"api/word/local-translate?word={Uri.EscapeDataString(sourceWord)}&direction={direction}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();

            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
            if (dict != null && dict.TryGetValue("result", out var translated))
            {
                return translated;
            }

            return null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient.Dispose();
                }
                _disposed = true;
            }
        }
    }
}