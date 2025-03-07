namespace LearningBotCore.exceptions;

/// <summary>
/// Исключение, которое выбрасывается, когда слово уже есть в словаре пользователя.
/// </summary>
/// <param name="message">Сообщение об ошибке. По умолчанию: "Слово уже есть в вашем словаре."</param>
public class WordAlreadyInVocabularyException(string? message = "Слово уже есть в вашем словаре.") : Exception(message);