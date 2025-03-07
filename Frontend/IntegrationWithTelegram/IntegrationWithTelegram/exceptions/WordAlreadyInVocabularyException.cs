namespace IntegrationWithTelegram.exceptions;

/// <summary>
/// Исключение, которое выбрасывается, когда пользователь пытается добавить слово, которое уже существует в его словаре.
/// </summary>
public class WordAlreadyInVocabularyException(string? message = "Слово уже есть в вашем словаре.") : Exception(message)
{
    // Конструктор принимает необязательный параметр message, который по умолчанию равен "Слово уже есть в вашем словаре."
    // Если сообщение не передано, используется значение по умолчанию.
}