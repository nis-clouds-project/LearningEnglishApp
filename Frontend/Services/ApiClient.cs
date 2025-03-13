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
                
                var error = await response.Content.ReadAsStringAsync();
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
        public async Task<List<Word>?> GetLearnedWordsAsync(long userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Word/learned?userId={userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var words = await response.Content.ReadFromJsonAsync<List<Word>>();
                    return words;
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
                
                var error = await response.Content.ReadAsStringAsync();
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
                var result = response.IsSuccessStatusCode;
                return result;
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
        public async Task<bool> AddUserAsync(long userId)
        {
            try
            {
                var existsResponse = await _httpClient.GetAsync($"api/user/{userId}");
                if (existsResponse.IsSuccessStatusCode)
                {
                    return true;
                }

                var endpoint = "api/user/add";
                
                var content = new StringContent(userId.ToString(), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
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
        /// Получает список слов в категории.
        /// </summary>
        /// <param name="categoryId">Идентификатор категории</param>
        /// <returns>Список слов в категории или null в случае ошибки</returns>
        public async Task<List<Word>?> GetWordsByCategoryAsync(long categoryId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Word/category/{categoryId}");
                
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
        /// Получает случайное слово для изучения.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="categoryId">Идентификатор категории (опционально).</param>
        /// <returns>Случайное слово или null в случае ошибки.</returns>
        public async Task<Word?> GetRandomWordAsync(long userId, long? categoryId)
        {
            try
            {
                if (categoryId.HasValue)
                {
                    var categories = await GetCategoriesAsync();
                    if (categories?.Any(c => c.Id == categoryId) != true)
                    {
                        return null;
                    }

                    var categoryWords = await GetWordsByCategoryAsync(categoryId.Value);
                    if (categoryWords == null || !categoryWords.Any())
                    {
                        return null;
                    }

                    var learnedWords = await GetLearnedWordsAsync(userId);
                    var learnedWordIds = learnedWords?.Select(w => w.Id).ToHashSet() ?? new HashSet<long>();

                    var availableWords = categoryWords.Where(w => !learnedWordIds.Contains(w.Id)).ToList();
                    if (!availableWords.Any())
                    {
                        return null;
                    }

                }

                var url = categoryId.HasValue 
                    ? $"api/Word/random?userId={userId}&categoryId={categoryId}"
                    : $"api/Word/random?userId={userId}";
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                
                var word = await response.Content.ReadFromJsonAsync<Word>();
                if (word == null)
                {
                    return null;
                }
                
                return word;
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
        public async Task<bool> AddWordToVocabularyAsync(long userId, int wordId)
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
        public async Task<Word?> GetWordByIdAsync(int wordId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/word/{wordId}");
                
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
        /// Сохраняет текущую категорию обучения пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="categoryId">Идентификатор категории</param>
        public async Task<bool> SaveUserLearningCategoryAsync(long userId, long categoryId)
        {
            try
            {
                var userResponse = await _httpClient.GetAsync($"api/User/{userId}");
                if (!userResponse.IsSuccessStatusCode)
                {
                    return false;
                }

                var user = await userResponse.Content.ReadFromJsonAsync<User>();
                if (user == null)
                {
                    return false;
                }

                user.current_learning_category = categoryId;

                var content = JsonContent.Create(user);
                var response = await _httpClient.PutAsync($"api/User/{userId}", content);
                
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
        /// Получает текущую категорию обучения пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <returns>ID категории или null, если категория не установлена</returns>
        public async Task<long?> GetUserLearningCategoryAsync(long userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/User/{userId}");
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var user = await response.Content.ReadFromJsonAsync<User>();
                if (user?.current_learning_category == null)
                {
                    return null;
                }

                return user.current_learning_category;
            }
            catch (Exception ex)
            {
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