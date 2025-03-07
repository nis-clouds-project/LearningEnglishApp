using System.Net;
using IntegrationWithTelegram.exceptions;

namespace IntegrationWithTelegram.managers;

/// <summary>
/// Статический класс для обработки исключений на основе HTTP-ответов.
/// </summary>
public static class ExceptionHandler
{
    /// <summary>
    /// Проверяет HTTP-ответ и выбрасывает соответствующее исключение, если статус ответа указывает на ошибку.
    /// </summary>
    /// <param name="response">HTTP-ответ, который необходимо проверить.</param>
    /// <exception cref="Exception">Выбрасывается, если произошла внутренняя ошибка сервера.</exception>
    /// <exception cref="NotFoundException">Выбрасывается, если запрашиваемый ресурс не найден.</exception>
    /// <exception cref="ArgumentException">Выбрасывается, если запрос некорректен.</exception>
    /// <exception cref="WordAlreadyInVocabularyException">Выбрасывается, если слово уже есть в словаре пользователя.</exception>
    /// <exception cref="NoWordsAvailableException">Выбрасывается, если в категории нет доступных слов.</exception>
    public static void ValidException(HttpResponseMessage response)
    {
        // Если статус ответа успешный, завершаем выполнение метода.
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        // Получаем код статуса HTTP-ответа.
        HttpStatusCode code = response.StatusCode;

        // Читаем тело ответа для получения дополнительной информации об ошибке.
        string body = response.Content.ReadAsStringAsync().Result;

        // В зависимости от кода статуса выбрасываем соответствующее исключение.
        switch (code)
        {
            case HttpStatusCode.NotFound:
                throw new NotFoundException(body);

            case HttpStatusCode.BadRequest:
                throw new ArgumentException("Некорректный запрос");

            case HttpStatusCode.Forbidden:
                throw new WordAlreadyInVocabularyException();

            case HttpStatusCode.Conflict:
                throw new NoWordsAvailableException();

            // Можно добавить обработку других кодов статуса, если это необходимо.
            default:
                throw new Exception($"Непредвиденная ошибка.");
        }
    }
}