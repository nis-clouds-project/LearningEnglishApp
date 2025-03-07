namespace IntegrationWithTelegram.exceptions;

/// <summary>
/// Исключение, которое выбрасывается, когда в указанной категории нет доступных слов.
/// </summary>
public class NoWordsAvailableException(string? message = "Слов нет в этой категории.") : Exception(message)
{
    // Конструктор принимает необязательный параметр message, который по умолчанию равен "Слов нет в этой категории."
    // Если сообщение не передано, используется значение по умолчанию.
}