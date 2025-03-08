namespace Frontend.Exceptions
{
    /// <summary>
    /// Исключение, которое выбрасывается, когда слово не найдено в базе данных.
    /// </summary>
    /// <param name="message">Сообщение об ошибке. По умолчанию: "Слово не найдено в нашей базе."</param>
    public class WordNotFoundException(string? message = "Слово не найдено в нашей базе.") : Exception(message);
}