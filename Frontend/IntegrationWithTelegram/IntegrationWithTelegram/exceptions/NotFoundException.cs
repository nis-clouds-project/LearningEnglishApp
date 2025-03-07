namespace IntegrationWithTelegram.exceptions;

/// <summary>
/// Исключение, которое выбрасывается, когда запрашиваемый объект не найден.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="NotFoundException"/> с указанным сообщением об ошибке.
    /// </summary>
    /// <param name="message">Сообщение об ошибке, объясняющее причину исключения.</param>
    public NotFoundException(string? message) : base(message)
    {
        // Базовый конструктор Exception инициализирует сообщение об ошибке.
        // Это сообщение может быть использовано для логирования или отображения пользователю.
    }
}