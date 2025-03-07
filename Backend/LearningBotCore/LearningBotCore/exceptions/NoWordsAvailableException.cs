namespace LearningBotCore.exceptions;

/// <summary>
/// Исключение, которое выбрасывается, когда в категории больше нет доступных слов для изучения.
/// </summary>
/// <param name="message">Сообщение об ошибке. По умолчанию: "Нет доступных слов в этой категории."</param>
public class NoWordsAvailableException(string? message = "В этой категории нет слов.") : Exception(message);