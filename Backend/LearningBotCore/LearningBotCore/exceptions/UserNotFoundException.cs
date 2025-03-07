namespace LearningBotCore.exceptions;

/// <summary>
/// Исключение, которое выбрасывается, когда пользователь не найден в системе.
/// </summary>
/// <param name="message">Сообщение об ошибке. По умолчанию: "Пользователь не найден."</param>
public class UserNotFoundException(string? message = "Пользователь не найден.") : Exception(message);